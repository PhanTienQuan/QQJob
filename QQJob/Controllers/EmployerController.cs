using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels.EmployerViewModels;
using System.Security.Claims;

namespace QQJob.Controllers
{
    [Authorize(Roles = "Employer")]
    public class EmployerController(IEmployerRepository employerRepository,IApplicationRepository applicationRepository,ISkillRepository skillRepository,IJobRepository jobRepository,ICloudinaryService cloudinaryService) : Controller
    {
        private readonly IEmployerRepository _employerRepository = employerRepository;
        private readonly IApplicationRepository _applicationRepository = applicationRepository;
        private readonly ISkillRepository _skillRepository = skillRepository;
        private readonly IJobRepository _jobRepository = jobRepository;
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _employerRepository.GetByIdAsync(id);
            if(user.Jobs == null)
            {
                ViewBag.JobCount = 0;
                ViewBag.ApplicantCount = 0;
                ViewBag.JobView = 0;
                ViewBag.Follows = 0;
                return View();
            }

            ViewBag.JobCount = user.Jobs.Count();
            ViewBag.ApplicantCount = user.Jobs.Sum(j => j.Applications.Count());
            ViewBag.JobView = user.Jobs.Sum(j => j.ViewCount);
            ViewBag.Follows = user.Follows.Count();

            var applications = new List<Application>();
            foreach(var job in user.Jobs)
            {
                var jobApplications = await _applicationRepository.GetApplications(job.JobId);
                applications.AddRange(jobApplications);
            }

            ViewBag.Application = applications;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var eId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(string.IsNullOrEmpty(eId))
            {
                return RedirectToAction("Login","Account");
            }

            var employer = await _employerRepository.GetByIdAsync(eId);
            if(employer == null)
            {
                return NotFound("Employer profile not found.");
            }

            List<SocialLink> socialLinks = JsonConvert.DeserializeObject<List<SocialLink>>(employer.User.SocialLink) ?? new List<SocialLink>();
            var model = new EmployerProfileViewModel
            {
                Id = employer.EmployerId,
                Avatar = employer.User.Avatar,
                FullName = employer.User.FullName,
                Email = employer.User.Email,
                PhoneNumber = employer.User.PhoneNumber,
                Website = employer.Website,
                FoundedDate = employer.FoundedDate,
                CompanySize = employer.CompanySize,
                ForPublicView = true,
                SocialLinks = socialLinks,
                CompanyField = employer.CompanyField
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(EmployerProfileViewModel model)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            var employer = await _employerRepository.GetByIdAsync(model.Id);
            if(employer == null)
            {
                return View(model);
            }

            bool isUpdated = false;
            // Update avatar if a new file is uploaded
            if(model.AvatarFile?.Length > 0)
            {
                try
                {
                    employer.User.Avatar = model.Avatar = await cloudinaryService.UpdateAvatar(model.AvatarFile,model.Id);
                    isUpdated = true;
                }
                catch
                {

                    ViewBag.Message = "Something happen to cloudinary server!";
                    return View(model);
                }
            }

            // Update fields if they differ from the current values
            employer.User.FullName = UpdateIfDifferent(employer.User.FullName,model.FullName.Trim(),ref isUpdated);
            employer.User.PhoneNumber = UpdateIfDifferent(employer.User.PhoneNumber,model.PhoneNumber?.Trim(),ref isUpdated);
            employer.Website = UpdateIfDifferent(employer.Website,model.Website?.Trim(),ref isUpdated);
            employer.FoundedDate = UpdateIfDifferent(employer.FoundedDate,model.FoundedDate,ref isUpdated);
            employer.CompanySize = UpdateIfDifferent(employer.CompanySize,model.CompanySize?.Trim(),ref isUpdated);
            employer.CompanyField = UpdateIfDifferent(employer.CompanyField,model.CompanyField,ref isUpdated);
            var socialLinksJson = JsonConvert.SerializeObject(model.SocialLinks);
            employer.User.SocialLink = UpdateIfDifferent(employer.User.SocialLink,socialLinksJson,ref isUpdated);


            if(isUpdated)
            {
                _employerRepository.Update(employer);
                await _employerRepository.SaveChangesAsync();
                TempData["Message"] = JsonConvert.SerializeObject(new { message = "Profile updated successfully!",type = "success" });
            }
            else
            {
                TempData["Message"] = JsonConvert.SerializeObject(new { message = "No changes detected.",type = "none" });
            }

            model.SocialLinks = JsonConvert.DeserializeObject<List<SocialLink>>(employer.User.SocialLink) ?? new List<SocialLink>();
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public async Task<IActionResult> JobsPosted()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var jobs = await _jobRepository.FindAsync(j => j.EmployerId == id);

            var postedJobViewModels = jobs
                .Take(3)
                .Select(job => new PostedJobViewModel()
                {
                    Id = job.JobId,
                    Title = job.Title,
                    Address = job.Address,
                    JobDes = JsonConvert.DeserializeObject<JobDescription>(job.JobDescription),
                    Open = job.PostDate,
                    Close = job.CloseDate,
                    AppliedCount = job.Applications != null ? job.Applications.Count() : 0,
                    Status = job.Status,
                })
                .ToList();

            // Pass the list to the view
            ViewBag.PostedJobs = postedJobViewModels;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PostJob()
        {
            var skills = await _skillRepository.GetAllAsync();
            ViewBag.SkillList = skills.Select(skill => new
            {
                Id = skill.SkillId,
                Name = skill.SkillName
            }).ToList();
            return View(new PostJobViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> PostJob(PostJobViewModel model)
        {
            try
            {
                var skills = await _skillRepository.GetAllAsync();

                ViewBag.SkillList = skills.Select(skill => new
                {
                    Id = skill.SkillId,
                    Name = skill.SkillName
                }).ToList();

                if(!ModelState.IsValid)
                {
                    return View(model);
                }

                var jobJD = new
                {
                    Descriptions = model.Description,
                    Responsibilities = model.Responsibilities,
                    Requirements = model.Requirements,
                    WorkingSche = model.CusWorkingSche ?? model.WorkingSche,
                };

                var selectedSkill = new List<Skill>();

                foreach(var id in model.SelectedSkill.Split(","))
                {
                    selectedSkill.Add(await _skillRepository.GetByIdAsync(int.Parse(id)));
                }

                var eId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var confirmed = (await _employerRepository.GetByIdAsync(eId)).User.IsVerified;
                var status = confirmed == Status.Approved ? Status.Approved : Status.Pending;

                Job newJob = new()
                {
                    EmployerId = eId,
                    Title = model.Title,
                    JobDescription = JsonConvert.SerializeObject(jobJD),
                    Address = model.CustomLocation ?? model.Location,
                    Experience = model.CusExperience ?? model.Experience,
                    Salary = model.Salary,
                    Benefits = model.Benefits,
                    Qualification = model.Qualification,
                    OpenPosition = model.Opening,
                    PostDate = DateTime.Now,
                    CloseDate = model.Close.ToDateTime(TimeOnly.MinValue),
                    Skills = selectedSkill,
                    Status = status
                };

                await _jobRepository.AddAsync(newJob);
                await _jobRepository.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString() + "=====> " + ex.Message);
            }

            return RedirectToAction("Index");
        }

        [NonAction]
        private T UpdateIfDifferent<T>(T currentValue,T newValue,ref bool isUpdated)
        {
            if(!EqualityComparer<T>.Default.Equals(currentValue,newValue))
            {
                isUpdated = true;
                return newValue;
            }
            return currentValue;
        }
    }

}
