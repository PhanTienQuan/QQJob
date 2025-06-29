using QQJob.Dtos;
using QQJob.Models;

namespace QQJob.Areas.Admin.ViewModels
{
    public class HomeViewModel
    {
        public IEnumerable<Job> NewJobs { get; set; } = [];
        public IEnumerable<AppUser> NewUsers { get; set; } = [];
        public IEnumerable<Job> RecentJobs { get; set; } = [];
        public IEnumerable<AppUser> RecentUsers { get; set; } = [];
        public IEnumerable<Application> RecentApplications { get; set; } = [];
        public int PendingJobs { get; set; }
        public int TotalJobPosting { get; set; }
        public List<StatsDto> Stats { get; set; } = [];
    }
}
