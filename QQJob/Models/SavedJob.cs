using System.ComponentModel.DataAnnotations;

namespace QQJob.Models
{
    public class SavedJob
    {
        [Key]
        public int SaveJobId { get; set; }
        public int JobId { get; set; }
        public Job Job { get; set; }
        public string CandidateId { get; set; }
        public Candidate Candidate { get; set; }
        public DateTime SaveDate { get; set; }
    }
}
