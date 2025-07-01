using QQJob.Models;

namespace QQJob.ViewModels
{
    public class EmployerDetailViewModel
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string AvatarUrl { get; set; }
        public string? CompanyField { get; set; }
        public IEnumerable<Job> Jobs { get; set; } = [];
        public DateTime? FoundedDate { get; set; }
        public string? Description { get; set; }
        public string? CompanySize { get; set; }
        public string? Spending { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public bool IsFollowed { get; set; }
    }
}
