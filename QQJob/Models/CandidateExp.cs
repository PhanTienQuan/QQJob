namespace QQJob.Models
{
    public class CandidateExp
    {
        public int CandidateExpId { get; set; }
        public string CandidateId { get; set; }
        public string ExpTitle { get; set; }
        public Candidate Candidate { get; set; }
        public string? Company { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }
}
