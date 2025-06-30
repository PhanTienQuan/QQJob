using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels;

namespace QQJob.Controllers
{
    public class HomeController(
        UserManager<AppUser> userManager,
        IEmployerRepository employerRepository,
        IJobRepository jobRepository
        ):Controller
    {

        public async Task<IActionResult> Index()
        {
            if(User.Identity != null && User.Identity.IsAuthenticated)
            {
                if((await userManager.FindByNameAsync(User.Identity.Name)) == null)
                {
                    return RedirectToAction("Logout",new { controller = "Account" });
                }
            }
            var (jobs, pagingModel) = await jobRepository.GetJobsAsync(1,6,j => j.Status == Status.Approved);
            ViewBag.ViewJobs = jobs;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> EmployerList(EmployerListViewModel employerListViewModel)
        {
            employerListViewModel.Paging ??= new PagingModel
            {
                CurrentPage = 1,
                PageSize = 5
            };
            DateTime? date = null;
            if(employerListViewModel.SearchFoundedDate.HasValue)
            {
                var year = employerListViewModel.SearchFoundedDate.Value;
                date = new(year,1,1);
            }

            var (employers, paging) = await employerRepository.GetJobsAsync(employerListViewModel.Paging.CurrentPage,employerListViewModel.Paging.PageSize,employerListViewModel.SearchEmployerName,employerListViewModel.SearchField,date);

            var viewModel = new EmployerListViewModel
            {
                Employers = employers.Select(e => new EmployerViewModel
                {
                    Id = e.EmployerId,
                    Name = e.User.FullName,
                    Slug = e.User.Slug,
                    Fields = e.CompanyField?.Split(",") ?? ["Not specify"],
                    PostedJobsCount = e.Jobs.Count,
                    AvatarUrl = e.User.Avatar
                }),
                Paging = paging,
            };
            if(employerListViewModel.Searching)
            {
                return PartialView("_EmployerList",viewModel);
            }
            return View(viewModel);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> EmployerDetail(string id,string slug)
        {
            var employer = await employerRepository.GetByIdAsync(id);
            if(employer == null)
            {
                return NotFound();
            }

            // If slug does not match — redirect to correct URL (SEO)
            if(employer.User.Slug != slug)
            {
                return RedirectToRoute(new
                {
                    id = employer.EmployerId,
                    slug = employer.User.Slug
                });
            }

            int? minimumSpending = 0;
            int? maximumSpending = 0;

            foreach(var job in employer.Jobs)
            {
                var (min, max) = Helper.Helper.ParseSalaryRange(job.Salary);
                minimumSpending += min;
                maximumSpending += max;
            }

            var currencies = employer.Jobs
                .Select(j => Helper.Helper.ParseCurrency(j.Salary))
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            string currencyLabel = currencies.Count > 0 ? string.Join("/",currencies) : "";

            var employerDetailViewModel = new EmployerDetailViewModel
            {
                Id = employer.EmployerId,
                Name = employer.User.FullName,
                CompanyField = employer.CompanyField ?? "Not specify",
                Jobs = employer.Jobs.Where(j => j.Status == Status.Approved),
                AvatarUrl = employer.User.Avatar,
                CompanySize = employer.CompanySize,
                Description = employer.Description,
                FoundedDate = employer.FoundedDate,
                Phone = employer.User.PhoneNumber ?? "Not specify",
                Spending = $"{minimumSpending:N0} - {maximumSpending:N0}" + (string.IsNullOrEmpty(currencyLabel) ? "" : $" {currencyLabel}"),
                Website = employer.Website
            };
            return View(employerDetailViewModel);
        }
    }
}
