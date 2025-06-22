namespace QQJob.ViewModels.CandidateViewModels
{
    public class ProfileViewModel
    {
        public string Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Avatar { get; set; }
        public IFormFile? AvatarFile { get; set; }
        public string? JobTitle { get; set; }
        public string? Description { get; set; }
        public string? WorkingType { get; set; }
        public string? ResumeUrl { get; set; }
    }
}
