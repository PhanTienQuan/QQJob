using Microsoft.AspNetCore.Identity;

namespace QQJob.Models
{
    public class AppUser : IdentityUser 
    {
        public string? Avatar { get; set; }
        public string? Slug { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsVerified { get; set; }
        public bool IsPremium { get; set; }
        public DateTime? LastLogin { get; set; }

        // Navigation Properties
        public Employer? Employer { get; set; }
        public Candidate? Candidate { get; set; }
    }
}
