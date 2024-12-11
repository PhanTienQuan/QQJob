using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QQJob.Models
{
    public class Candidate
    {
        [Key]
        [Column("CandidateId")] // Đổi tên UserId thành CandidateId
        public string CandidateId { get; set; } // Đây vẫn ánh xạ với UserId của AspNetUsers
        public string? JobTitle { get; set; }
        public string? Description { get; set; }
        public string? WorkingType { get; set; }
        public string? ResumePath { get; set; }
        public string? SocialLink { get; set; }

        // Navigation Property
        public AppUser User { get; set; }
        public ICollection<Education> Educations { get; set; }
        public ICollection<Award> Awards { get; set; }
        public ICollection<CandidateExp> CandidateExps { get; set; }
        public ICollection<Application> Applications { get; set; }
        public ICollection<Follow> Follows { get; set; }
        public ICollection<ViewJobHistory> ViewJobHistories { get; set; }
        public ICollection<SavedJob> SavedJobs { get; set; }
        public ICollection<Skill> Skills { get; set; } // N:N với Skill
    }
}
