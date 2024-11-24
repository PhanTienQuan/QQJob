namespace QQJob.Models
{
    public class Application
    {
        public int ApplicationId { get; set; }
        public int JobId { get; set; }
        public Job Job { get; set; }
        public string CandidateId { get; set; }
        public Candidate Candidate { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string? CoverLetter { get; set; }
        public int Status { get; set; }
    }
}
