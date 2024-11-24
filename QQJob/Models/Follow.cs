namespace QQJob.Models
{
    public class Follow
    {
        public long Id { get; set; }
        public string CandidateId { get; set; }
        public Candidate Candidate { get; set; }
        public string EmployerId { get; set; }
        public Employer Employer { get; set; }
        public DateTime FollowOn { get; set; }
    }
}
