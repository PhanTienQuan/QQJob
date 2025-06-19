using QQJob.Models.Enum;

namespace QQJob.Models
{
    public class Job
    {
        public int JobId { get; set; }
        public string? JobTitle { get; set; }
        public string? Salary { get; set; }
        public string? SalaryType { get; set; } // e.g., Year, Hour, etc.
        public string? City { get; set; }
        public string? LocationRequirement { get; set; } // e.g., Remote, Onsite, Hybrid
        public string? JobType { get; set; } // e.g., Fulltime, Part-time, Contract
        public string? ExperienceLevel { get; set; } // e.g., Junior, Mid, Senior
        public string? Description { get; set; }
        public DateTime PostDate { get; set; }
        public DateTime CloseDate { get; set; }
        public DateTime? UpdateAt { get; set; }
        public Status Status { get; set; }
        public string? Slug { get; set; }
        public int Opening { get; set; }
        public int ViewCount { get; set; }
        // Foreign Key
        public string? EmployerId { get; set; }
        public Employer? Employer { get; set; }

        // Navigation Properties
        public ICollection<SavedJob>? SavedJobs { get; set; }
        public ICollection<Skill>? Skills { get; set; }
        public ICollection<Application>? Applications { get; set; }
    }
}
