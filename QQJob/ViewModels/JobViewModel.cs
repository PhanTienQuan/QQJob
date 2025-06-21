using QQJob.Models;
using QQJob.Models.Enum;

namespace QQJob.ViewModels
{
    public class JobViewModel
    {
        public int Id { get; set; }
        public string JobTitle { get; set; }
        public string City { get; set; }
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        public int AppliedCount { get; set; }
        public Status Status { get; set; }
        public IEnumerable<Skill> Skills { get; set; }
        public string? Salary { get; set; }
        public string? SalaryType { get; set; } // e.g., Year, Hour, etc.
        public string? LocationRequirement { get; set; } // e.g., Remote, Onsite, Hybrid
        public string? JobType { get; set; } // e.g., Fulltime, Part-time, Contract
        public string? ExperienceLevel { get; set; }
        public string? Slug { get; set; }
        public string AvatarUrl { get; set; }
    }
}
