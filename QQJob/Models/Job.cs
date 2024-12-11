using QQJob.Models.Enum;

namespace QQJob.Models
{
    public class Job
    {
        public int JobId { get; set; }
        public string? Title { get; set; }
        public string? Address { get; set; }
        public int Experience { get; set; }
        public string? Salary { get; set; }
        public string? JobDescription { get; set; }
        public string? Qualification { get; set; }
        public string? Benefits { get; set; }
        public DateTime PostDate { get; set; }
        public DateTime CloseDate { get; set; }
        public Status Status { get; set; }
        public string? Slug { get; set; }
        public int OpenPosition { get; set; }
        public long ViewCount { get; set; }

        // Foreign Key
        public string? EmployerId { get; set; }
        public Employer? Employer { get; set; }

        // Navigation Properties
        public ICollection<Skill>? Skills { get; set; }
        public ICollection<Application>? Applications { get; set; }
    }
}
