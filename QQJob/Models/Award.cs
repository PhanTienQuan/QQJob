namespace QQJob.Models
{
    public class Award
    {
        public int AwardId { get; set; }
        public string CandidateId { get; set; }
        public Candidate Candidate { get; set; }
        public string Title { get; set; }
        public int? Year { get; set; }
        public string? Description { get; set; }
    }
}
