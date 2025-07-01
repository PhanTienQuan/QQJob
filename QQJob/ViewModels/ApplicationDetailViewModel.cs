using QQJob.Models.Enum;

namespace QQJob.ViewModels
{
    public class ApplicationDetailViewModel
    {
        public required int ApplicationId { get; set; }
        public required string CandidateId { get; set; }
        public required string CandidateName { get; set; }
        public required string CandidateSlug { get; set; }
        public required string EmployerId { get; set; }
        public required string EmployerName { get; set; }
        public required int JobId { get; set; }
        public required string JobTitle { get; set; }
        public required string JobSlug { get; set; }
        public string? CoverLetter { get; set; }
        public string? ResumeUrl { get; set; }
        public string? ResumeSummary { get; set; }
        public required DateTime ApplicationDate { get; set; }
        public ApplicationStatus Status { get; set; }
        public required string CandidateAvatar { get; set; }
        public float? AIRanking { get; set; }
        public string? Phone { get; set; }

    }
}
