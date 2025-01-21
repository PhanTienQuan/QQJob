using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels;

namespace QQJob.Controllers
{
    public class JobsController(IJobRepository jobRepository):Controller
    {
        private readonly IJobRepository _jobRepository = jobRepository;
        public async Task<IActionResult> Index()
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
        public async Task<IActionResult> Detail(int id)
        {
            var job = await _jobRepository.GetByIdAsync(id);

            var jobDetailViewModel = new JobDetailViewModel()
            {
                Id = job.JobId,
                Title = job.Title,
                Address = job.Address,
                JobDes = JsonConvert.DeserializeObject<JobDescription>(job.JobDescription),
                Open = job.PostDate,
                Close = job.CloseDate,
                AppliedCount = job.Applications != null ? job.Applications.Count() : 0,
                Status = job.Status,
                Skills = job.Skills,
                Salary = job.Salary,
                Opening = job.OpenPosition,
                Experience = job.Experience,
                Qualification = job.Qualification,
                Benefits = job.Benefits
            };

            ViewBag.Job = jobDetailViewModel;
            return View();
        }
    }
}
