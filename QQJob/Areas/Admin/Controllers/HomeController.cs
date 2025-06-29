using Microsoft.AspNetCore.Mvc;
using QQJob.Areas.Admin.ViewModels;
using QQJob.Dtos;
using QQJob.Repositories.Interfaces;

namespace QQJob.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController(ILogger<HomeController> logger,IJobRepository jobRepository,IAppUserRepository appUserRepository,IApplicationRepository applicationRepository):Controller
    {
        private readonly ILogger<HomeController> _logger = logger;

        public async Task<IActionResult> Index()
        {
            var jobs = await jobRepository.GetAllAsync();
            var users = await appUserRepository.GetAllAsync();
            var applications = await applicationRepository.GetAllAsync();

            var jobGroups = jobs
                .GroupBy(j => new { j.PostDate.Year,j.PostDate.Month })
                .Select(g => new { g.Key.Year,g.Key.Month,Posts = g.Count() });

            var userGroups = users
                .GroupBy(u => new { u.CreatedAt.Year,u.CreatedAt.Month })
                .Select(g => new { g.Key.Year,g.Key.Month,Users = g.Count() });

            var applicationGroups = applications
                .GroupBy(a => new { a.ApplicationDate.Year,a.ApplicationDate.Month })
                .Select(g => new { g.Key.Year,g.Key.Month,Application = g.Count() });

            var months = jobGroups.Select(g => new { g.Year,g.Month })
                .Union(userGroups.Select(g => new { g.Year,g.Month }))
                .Union(applicationGroups.Select(g => new { g.Year,g.Month }))
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            var stats = new List<StatsDto>();
            foreach(var m in months)
            {
                var job = jobGroups.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);
                var user = userGroups.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);
                var app = applicationGroups.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);

                stats.Add(new StatsDto
                {
                    Month = $"{m.Year}-{m.Month:00}",
                    Posts = job?.Posts ?? 0,
                    Users = user?.Users ?? 0,
                    Applications = app?.Application ?? 0
                });
            }

            for(int i = 1;i < stats.Count;i++)
            {
                var prev = stats[i - 1];
                var curr = stats[i];
                curr.PostGrowth = prev.Posts == 0 ? (double?)null : ((double)(curr.Posts - prev.Posts) / prev.Posts * 100);
                curr.UserGrowth = prev.Users == 0 ? (double?)null : ((double)(curr.Users - prev.Users) / prev.Users * 100);
                curr.ApplicationGrowth = prev.Applications == 0 ? (double?)null : ((double)(curr.Applications - prev.Applications) / prev.Applications * 100);
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var homeViewModel = new HomeViewModel
            {
                NewJobs = await jobRepository.FindAsync(j => j.PostDate >= today && j.PostDate < tomorrow),
                NewUsers = await appUserRepository.FindAsync(u => u.CreatedAt >= today && u.CreatedAt < tomorrow),
                RecentJobs = [.. jobs.OrderByDescending(j => j.PostDate).Take(5)],
                RecentUsers = [.. users.OrderByDescending(u => u.CreatedAt).Take(5)],
                RecentApplications = [.. applications.OrderByDescending(a => a.ApplicationDate).Take(5)],
                PendingJobs = jobs.Where(j => j.Status == Models.Enum.Status.Pending).Count(),
                TotalJobPosting = jobs.Count(),
                Stats = stats
            };
            return View(homeViewModel);
        }
    }
}
