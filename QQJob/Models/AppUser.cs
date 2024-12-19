using Microsoft.AspNetCore.Identity;
using QQJob.Models.Enum;

namespace QQJob.Models
{
    public class AppUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public string? Slug { get; set; }
        public DateTime CreatedAt { get; set; }
        public Status IsVerified { get; set; }
        public bool IsPremium { get; set; }
        public DateTime? LastLogin { get; set; }

        // Navigation Properties
        public Employer? Employer { get; set; }

        public Candidate? Candidate { get; set; }
    }
}