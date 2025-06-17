using QQJob.Models.Enum;

namespace QQJob.Dtos
{
    public class ApplicationDto
    {
        public string JobTitle { get; set; }
        public DateTime AppliedAt { get; set; }
        public ApplicationStatus Status { get; set; }
    }
}
