using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QQJob.Helper;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels;
using QQJob.ViewModels.EmployerViewModels;
using System.Security.Claims;

namespace QQJob.Controllers
{
    [Authorize(Roles = "Employer")]
    public class EmployerController(IEmployerRepository employerRepository,
        IApplicationRepository applicationRepository,
        ISkillRepository skillRepository,
        IJobRepository jobRepository,
        ICloudinaryService cloudinaryService,
        IChatSessionRepository chatSessionRepository,
        IAppUserRepository appUserRepository,
        IChatMessageRepository chatMessageRepository
        ):Controller
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

            List<SocialLink>? socialLinks = string.IsNullOrWhiteSpace(employer.User.SocialLink) ? new List<SocialLink>() : JsonConvert.DeserializeObject<List<SocialLink>>(employer.User.SocialLink);

            var model = new EmployerProfileViewModel
            {
                Id = employer.EmployerId,
                Avatar = employer.User.Avatar,
                FullName = employer.User.FullName,
                Email = employer.User.Email,
                PhoneNumber = employer.User.PhoneNumber,
                Website = employer.Website,
                FoundedDate = employer.FoundedDate != DateTime.MinValue ? employer.FoundedDate : null,
                CompanySize = employer.CompanySize,
                ForPublicView = true,
                SocialLinks = socialLinks,
                CompanyField = employer.CompanyField,
                IsVerified = employer.User.IsVerified
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
            employer.FoundedDate = UpdateIfDifferent(employer.FoundedDate,model.FoundedDate ??= DateTime.MinValue,ref isUpdated);
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

            model.SocialLinks = JsonConvert.DeserializeObject<List<SocialLink>>(employer.User.SocialLink) ?? [];
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public async Task<IActionResult> JobsPosted(int page = 1,int pageSize = 5)
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var (jobs, pagingModel) = await _jobRepository.GetJobsAsync(page,pageSize,j => j.EmployerId == id);

            var model = new PostedJobsViewModel()
            {
                Jobs = new List<PostedJobViewModel>()
            };

            foreach(var job in jobs)
            {
                model.Jobs.Add(new PostedJobViewModel
                {
                    Id = job.JobId,
                    Title = job.Title,
                    Address = job.Address,
                    JobDescription = JsonConvert.DeserializeObject<JobDescription>(job.JobDescription),
                    Open = job.PostDate,
                    Close = job.CloseDate,
                    AppliedCount = job.Applications != null ? job.Applications.Count() : 0,
                    Status = job.Status,
                });
            }

            model.Paging = pagingModel;
            //model.SearchValue = searchValue;
            //model.SearchStatus = searchStatus;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetJobListPartial(PagingModel paging,string? searchValue = null,Status? searchStatus = null,DateTime? fromDate = null,DateTime? toDate = null)
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var current = paging.CurrentPage;
            var (jobs, pagingModel) = await _jobRepository.GetJobsAsync(paging.CurrentPage,paging.PageSize,j => j.EmployerId == id,searchValue,searchStatus,fromDate,toDate);

            var model = new PostedJobsViewModel()
            {
                Jobs = new List<PostedJobViewModel>()
            };

            foreach(var job in jobs)
            {
                model.Jobs.Add(new PostedJobViewModel
                {
                    Id = job.JobId,
                    Title = job.Title,
                    Address = job.Address,
                    JobDescription = JsonConvert.DeserializeObject<JobDescription>(job.JobDescription),
                    Open = job.PostDate,
                    Close = job.CloseDate,
                    AppliedCount = job.Applications != null ? job.Applications.Count() : 0,
                    Status = job.Status,
                });
            }

            model.Paging = pagingModel;
            model.SearchValue = searchValue;
            model.SearchStatus = searchStatus;
            model.FromDate = fromDate;
            model.ToDate = toDate;

            return PartialView("_PostedJobList",model);
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
            var model = new PostJobViewModel()
            {
                Opening = 1
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> PostJob(PostJobViewModel model)
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

            if((model.WorkingType == null && model.CusWorkingType == null) || (model.Experience == null && model.CusExperience == null))
            {
                string errorMessage = string.Empty;
                if(model.WorkingType == null && model.CusWorkingType == null)
                {
                    errorMessage += "Working Type field";
                    ModelState.AddModelError("WorkingType","This field is required!");
                }
                if(model.Experience == null && model.CusExperience == null)
                {
                    errorMessage += errorMessage == string.Empty ? "Experience field" : ", Experience field";
                    ModelState.AddModelError("Experience","This field is required!");
                }
                errorMessage += " is required!";
                TempData["Message"] = JsonConvert.SerializeObject(new { message = errorMessage,type = "error" });
                return View(model);
            }

            var jobJD = new
            {
                Descriptions = model.Description,
                model.Responsibilities,
                model.Requirements,
                WorkingType = model.CusWorkingType ?? model.WorkingType,
            };

            var selectedSkill = new List<Skill>();

            foreach(var id in model.SelectedSkill.Split(","))
            {
                selectedSkill.Add(await _skillRepository.GetByIdAsync(int.Parse(id)));
            }

            var eId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var confirmed = (await _employerRepository.GetByIdAsync(eId)).User.IsVerified;
            var status = confirmed == UserStatus.Verified ? Status.Approved : Status.Pending;

            Job newJob = new()
            {
                EmployerId = eId,
                Title = model.Title,
                JobDescription = JsonConvert.SerializeObject(jobJD),
                Address = model.CustomLocation ?? model.Location,
                Experience = (float)(model.CusExperience ?? model.Experience),
                Salary = model.Salary,
                Benefits = model.Benefits,
                Qualification = model.Qualification,
                OpenPosition = model.Opening,
                PostDate = DateTime.Now,
                CloseDate = (DateTime)model.Close,
                Skills = selectedSkill,
                Status = status,
                WorkingHours = model.WorkingHours,
                WorkingType = model.CusWorkingType ?? model.WorkingType,
                PayType = model.CusPayType ?? model.PayType,
            };

            await _jobRepository.AddAsync(newJob);
            await _jobRepository.SaveChangesAsync();
            var message = "Post successfully!" + (confirmed != UserStatus.Verified ? " Wait for admin to verify your post" : "");
            TempData["Message"] = JsonConvert.SerializeObject(new { message,type = "success" });
            return RedirectToAction("JobsPosted");
        }

        [HttpGet]
        public async Task<IActionResult> EditJob(int id)
        {
            var job = await _jobRepository.GetByIdAsync(id);

            var jobDetailViewModel = new EditJobViewModel()
            {
                Id = job.JobId,
                Title = job.Title,
                Address = job.Address,
                JobDescription = JsonConvert.DeserializeObject<JobDescription>(job.JobDescription),
                Close = job.CloseDate,
                Salary = job.Salary,
                Opening = job.OpenPosition,
                Experience = job.Experience,
                Qualification = job.Qualification,
                Benefits = job.Benefits,

            };
            var skills = await _skillRepository.GetAllAsync();
            ViewBag.SkillList = skills.Select(skill => new
            {
                Id = skill.SkillId,
                Name = skill.SkillName
            }).ToList();

            ViewBag.InitSkills = skills.Where(skill => job.Skills.Contains(skill)).Select(skill => new
            {
                Id = skill.SkillId,
                Name = skill.SkillName
            }).ToList();

            return View(jobDetailViewModel);
        }
        [HttpPost]
        public async Task<IActionResult> EditJob(EditJobViewModel model)
        {
            var job = await _jobRepository.GetByIdAsync(model.Id);

            var jobDetailViewModel = new EditJobViewModel()
            {
                Id = job.JobId,
                Title = job.Title,
                Address = job.Address,
                JobDescription = JsonConvert.DeserializeObject<JobDescription>(job.JobDescription),
                Close = job.CloseDate,
                Salary = job.Salary,
                Opening = job.OpenPosition,
                Experience = job.Experience,
                Qualification = job.Qualification,
                Benefits = job.Benefits,
                SelectedSkill = string.Join(",",job.Skills.Select(skill => skill.SkillId))
            };

            if(ObjectComparer.AreEqual(model,jobDetailViewModel))
            {
                TempData["Message"] = JsonConvert.SerializeObject(new { message = "Nothing changed!",type = "none" });
                return RedirectToAction("EditJob");
            }

            var skills = await _skillRepository.GetAllAsync();
            ViewBag.SkillList = skills.Select(skill => new
            {
                Id = skill.SkillId,
                Name = skill.SkillName
            }).ToList();

            ViewBag.InitSkills = job.Skills.Select(skill => new
            {
                Id = skill.SkillId,
                Name = skill.SkillName
            }).ToList();

            if(!ModelState.IsValid)
            {
                TempData["Message"] = JsonConvert.SerializeObject(new { message = "Some field are wrong!",type = "error" });
                return View(model);
            }

            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Message()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessions = await chatSessionRepository.GetChatSession(userId,10,10);
            MessageViewModel messageViewModel = new MessageViewModel()
            {
                Sessions = sessions,
                CurrentChatSession = sessions != null ? sessions.FirstOrDefault() : new ChatSession(),
                CurrentUser = await appUserRepository.GetByIdAsync(userId),
            };
            return View(messageViewModel);
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
