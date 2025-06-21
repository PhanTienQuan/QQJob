using QQJob.Models.Enum;

namespace QQJob.ViewModels
{
    public class JobListViewModel
    {
        public IEnumerable<JobViewModel> Jobs { get; set; } = [];
        public PagingModel Paging { get; set; } = new PagingModel();
        public string? AiSearchQuery { get; set; }
        public string? JobTitle { get; set; }
        public string? City { get; set; }
        public string? ExperienceLevel { get; set; }
        public string? Salary { get; set; }
        public List<string>? Skills { get; set; }
    }
}
