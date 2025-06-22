namespace QQJob.ViewModels.CandidateViewModels
{
    public class ApplyJobViewModel
    {
        public string CoverLetter { get; set; }
        public IFormFile CV { get; set; }
        public int JobId { get; set; }
    }
}
