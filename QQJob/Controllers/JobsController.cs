using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels;

namespace QQJob.Controllers
{
    public class JobsController(IJobRepository jobRepository,IEmployerRepository employerRepository,IJobSimilarityMatrixRepository jobSimilarityMatrixRepository):Controller
    {
        public IActionResult Index()
        {
            //var jobs = await _jobRepository.GetJobsAsync(1, 5);
            //var joblist = jobs
            //    .Select(job => new JobListViewModel()
            //    {
            //        Id = job.JobId,
            //        Title = job.Title,
            //        Address = job.Address,
            //        JobDes = JsonConvert.DeserializeObject<JobDescription>(job.JobDescription),
            //        Open = job.PostDate,
            //        Close = job.CloseDate,
            //        AppliedCount = job.Applications != null ? job.Applications.Count() : 0, // Avoid .ToList() here for better performance
            //        Status = job.Status,
            //        Skills = job.Skills.Take(3).ToList()
            //    })
            //    .ToList();

            //// Pass the list to the view
            //ViewBag.Jobs = joblist;
            return View();
        }
        [HttpGet]
        [Route("jobs/{id}/{slug}")]
        public async Task<IActionResult> Detail(int id,string slug)
        {
            var job = await jobRepository.GetByIdAsync(id);
            if(job == null)
            {
                return NotFound();
            }

            // If slug does not match — redirect to correct URL (SEO)
            if(job.Slug != slug)
            {
                return RedirectToRoute(new
                {
                    id = job.JobId,
                    slug = job.Slug
                });
            }

            var employer = await employerRepository.GetByIdAsync(job.EmployerId);
            var relatedJobIds = await jobSimilarityMatrixRepository.GetRelatedJobIdsAsync(job.JobId);
            var relatedJobs = await jobRepository.FindJobs(j => relatedJobIds.Contains(j.JobId));
            var relatedJobView = relatedJobs.Select(j => new RelatedJobViewModel()
            {
                Id = j.JobId,
                Avatar = j.Employer.User.Avatar,
                City = j.City,
                JobType = j.JobType,
                Title = j.JobTitle,
                Skills = j.Skills.Select(s => s.SkillName).Take(3).ToList() ?? [],
                Opening = j.Opening,
                Slug = j.Slug
            }).ToList();

            var jobDetailViewModel = new JobDetailViewModel()
            {
                Id = job.JobId,
                EmployerId = job.EmployerId,
                JobTitle = job.JobTitle,
                City = job.City,
                Description = job.Description,
                PostDate = job.PostDate,
                CloseDate = job.CloseDate,
                AppliedCount = job.Applications != null ? job.Applications.Count() : 0,
                Status = job.Status,
                Skills = job.Skills,
                Salary = job.Salary,
                Opening = job.Opening,
                ExperienceLevel = job.ExperienceLevel,
                Website = employer.Website,
                ImgUrl = employer.User.Avatar,
                JobType = job.JobType,
                LocationRequirement = job.LocationRequirement,
                SalaryType = job.SalaryType,
                RelatedJobs = relatedJobView,
                SocialLinks = string.IsNullOrWhiteSpace(employer.User.SocialLink) ? [] : JsonConvert.DeserializeObject<List<SocialLink>>(employer.User.SocialLink)
            };
            return View(jobDetailViewModel);
        }

    }
}
