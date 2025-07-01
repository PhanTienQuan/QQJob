namespace QQJob.ViewModels.CandidateViewModels
{
    public class CandidateDetailViewModel
    {
        public string CandidateId { get; set; }
        public string FullName { get; set; }
        public string Avatar { get; set; }
        public string JobTitle { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string WorkingType { get; set; }
        public int FollowerCount { get; set; }
        public List<string> Skills { get; set; }
        public List<QQJob.Models.Education> Educations { get; set; }
        public List<QQJob.Models.CandidateExp> Experiences { get; set; }
        public List<QQJob.Models.Award> Awards { get; set; }
        public string ResumeUrl { get; set; }
    }
}