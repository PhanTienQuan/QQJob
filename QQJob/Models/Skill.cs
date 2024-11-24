namespace QQJob.Models
{
    public class Skill
    {
        public int SkillId { get; set; }
        public string? SkillName { get; set; }
        public string? Description { get; set; }

        // Navigation Properties
        public ICollection<Candidate> Candidates { get; set; } // Quan hệ N:N với Candidate
        public ICollection<Job> Jobs { get; set; }
    }
}
