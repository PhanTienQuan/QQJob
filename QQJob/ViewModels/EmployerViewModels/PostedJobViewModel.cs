using QQJob.Models.Enum;

namespace QQJob.ViewModels.EmployerViewModels
{
    public class PostedJobViewModel
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? City { get; set; }
        public string? Description { get; set; }
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        public int AppliedCount { get; set; }
        public Status Status { get; set; }
        public required string Slug { get; set; }
    }
}
