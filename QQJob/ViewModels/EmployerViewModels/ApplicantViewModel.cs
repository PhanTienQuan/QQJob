using QQJob.Models;
using QQJob.Models.Enum;

namespace QQJob.ViewModels.EmployerViewModels
{
    public class ApplicantViewModel
    {
        public int ApplicationId { get; set; }
        public int JobId { get; set; }
        public string CandidateId { get; set; }
        public DateTime ApplicationDate { get; set; }
        public ApplicationStatus Status { get; set; }
        public IEnumerable<Skill> Skills { get; set; } = [];
        public string JobTitle { get; set; }
        public string ApplicantSlug { get; set; }
        public string JobSlug { get; set; }
        public string CandidateName { get; set; }
        public string CandidateAvatarUrl { get; set; }
        public float AiRanking { get; set; }
        public string ResumeUrl { get; set; }
        public string FileName { get; set; }
    }
}
