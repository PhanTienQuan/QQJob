using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;

namespace QQJob.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class JobController(IJobRepository jobRepository, IEmployerRepository employerRepository) : Controller
    {
        private readonly IJobRepository _jobRepository = jobRepository;
        private readonly IEmployerRepository _employerRepository = employerRepository;
        public async Task<IActionResult> Index()
        {
            var jobs = await _jobRepository.GetJobsAsync();
            return View(jobs);
        }
        public async Task<IActionResult> Detail(string id)
        {
            var jobs = await _jobRepository.GetByIdAsync(id);
            return View(jobs);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var ids = await _employerRepository.GetAllEmployerIdAsync();
            ViewBag.EmployerId = ids.Select(id => new SelectListItem
            {
                Value = id,
                Text = id
            });


            ViewBag.StatusList = Enum.GetValues(typeof(Status))
                        .Cast<Status>()
                        .Select(s => new SelectListItem
                        {
                            Value = ((int)s).ToString(),
                            Text = s.ToString()
                        })
                        .ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Job job)
        {
            try
            {
                var ids = await _employerRepository.GetAllEmployerIdAsync();
                job.Employer = await _employerRepository.GetByIdAsync(job.EmployerId);

                ViewBag.EmployerId = ids.Select(id => new SelectListItem
                {
                    Value = id,
                    Text = id
                });


                ViewBag.StatusList = Enum.GetValues(typeof(Status))
                            .Cast<Status>()
                            .Select(s => new SelectListItem
                            {
                                Value = ((int)s).ToString(),
                                Text = s.ToString()
                            })
                            .ToList();

                if(ModelState.IsValid)
                {
                    await _jobRepository.AddAsync(job);
                    await _jobRepository.SaveChangesAsync();
                    return RedirectToAction("Index");
                }

                return View(job);
            }
            catch(Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(job);
            }

        }
    }
}
