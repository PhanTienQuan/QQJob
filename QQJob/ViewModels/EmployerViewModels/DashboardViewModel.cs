using QQJob.Models;

namespace QQJob.ViewModels.EmployerViewModels
{
    public class DashboardViewModel
    {
        public int PostedJobCount { get; set; }
        public int ApplicationCount { get; set; }
        public int ViewCount { get; set; }
        public int FollowerCount { get; set; }
        public IEnumerable<Application>? RecentApplicants { get; set; }

    }
}
