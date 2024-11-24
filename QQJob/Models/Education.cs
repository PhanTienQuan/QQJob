namespace QQJob.Models
{
    public class Education
    {
        public int EducationId { get; set; }
        public string CandidateId { get; set; }
        public Candidate Candidate { get; set; }
        public string UniversityName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }
}
