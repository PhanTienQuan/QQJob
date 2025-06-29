using QQJob.Models.Enum;

namespace QQJob.Models
{
    public class Application
    {
        public int ApplicationId { get; set; }

        public int JobId { get; set; }
        public virtual Job Job { get; set; } = null!;

        public string CandidateId { get; set; } = null!;
        public virtual Candidate Candidate { get; set; } = null!;

        public DateTime ApplicationDate { get; set; }

        public string? CoverLetter { get; set; }

        public ApplicationStatus Status { get; set; }
        public float? AIRanking { get; set; }
    }

}
