using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Controllers
{
    [Authorize(Roles = "Employer")]
    public class EmployerController(IEmployerRepository employerRepository, IApplicationRepository applicationRepository) : Controller
    {
        private readonly IEmployerRepository _employerRepository = employerRepository;
        private readonly IApplicationRepository _applicationRepository = applicationRepository;
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;
            var user = await _employerRepository.GetEmployerByName(username);
            ViewBag.JobCount = user.Jobs.Count();
            ViewBag.ApplicantCount = user.Jobs.Sum(j => j.Applications.Count);
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

        public IActionResult EmployerProfile()
        {
            return View();
        }

        public IActionResult JobsPosted()
        {
            return View();
        }

        public IActionResult PostJob()
        {
            return View();
        }
    }

}
