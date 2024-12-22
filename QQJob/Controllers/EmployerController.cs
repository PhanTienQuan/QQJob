using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels;
using System.Security.Claims;

namespace QQJob.Controllers
{
    [Authorize(Roles = "Employer")]
    public class EmployerController(IEmployerRepository employerRepository, IApplicationRepository applicationRepository, ISkillRepository skillRepository, IJobRepository jobRepository) : Controller
    {
        private readonly IEmployerRepository _employerRepository = employerRepository;
        private readonly IApplicationRepository _applicationRepository = applicationRepository;
        private readonly ISkillRepository _skillRepository = skillRepository;
        private readonly IJobRepository _jobRepository = jobRepository;
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;
            var user = await _employerRepository.GetEmployerByName(username);
            ViewBag.JobCount = user.Jobs.Count();
            ViewBag.ApplicantCount = user.Jobs.Sum(j => j.Applications.ToList().Count);
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

        public async Task<IActionResult> EmployerProfile()
        {
            var eId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var employer = await _employerRepository.GetByIdAsync(eId);
            return View(employer);
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
                    JobDes = JsonConvert.DeserializeObject<JobDes>(job.JobDescription),
                    Open = job.PostDate,
                    Close = job.CloseDate,
                    AppliedCount = job.Applications != null ? job.Applications.Count() : 0, // Avoid .ToList() here for better performance
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
    }

}
