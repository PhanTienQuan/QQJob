using QQJob.Models;
using QQJob.Models.Enum;

namespace QQJob.ViewModels
{
    public class JobDetailViewModel
    {
        public int Id { get; set; }
        public string EmployerId { get; set; }
        public string JobTitle { get; set; }
        public string City { get; set; }
        public string Description { get; set; }
        public DateTime PostDate { get; set; }
        public DateTime CloseDate { get; set; }
        public int AppliedCount { get; set; }
        public Status Status { get; set; }
        public IEnumerable<Skill> Skills { get; set; }
        public string Salary { get; set; }
        public string SalaryType { get; set; }
        public int Opening { get; set; }
        public string ExperienceLevel { get; set; }
        public string ImgUrl { get; set; }
        public string? Website { get; set; }
        public string? LocationRequirement { get; set; }
        public string? JobType { get; set; }
        public List<RelatedJobViewModel> RelatedJobs { get; set; } = new List<RelatedJobViewModel>();
    }
}
