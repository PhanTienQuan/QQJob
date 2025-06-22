using QQJob.Models;
using System.Collections.Generic;

namespace QQJob.ViewModels.CandidateViewModels
{
    public class DashboardViewModel
    {
        public int ApplicationCount { get; set; }
        public int SavedJobCount { get; set; }
        public int UnreadMessageCount { get; set; }
        public int UnreadNotificationCount { get; set; }
        public int FollowerCount { get; set; }
        public int ViewCount { get; set; }
        public IEnumerable<Application>? RecentApplications { get; set; }
    }
}