using QQJob.Models.Enum;

namespace QQJob.ViewModels.EmployerViewModels
{
    public class EmployerProfileViewModel
    {
        public string Id { get; set; }
        public string? Avatar { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public DateTime? FoundedDate { get; set; }
        public string? CompanySize { get; set; }
        public bool ForPublicView { get; set; }
        public string? CompanyField { get; set; }
        public UserStatus IsVerified { get; set; }
        public List<SocialLink>? SocialLinks { get; set; }
        public IFormFile? AvatarFile { get; set; }
    }
}
