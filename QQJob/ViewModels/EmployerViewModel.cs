namespace QQJob.ViewModels
{
    public class EmployerViewModel
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public IEnumerable<string>? Fields { get; set; }
        public int PostedJobsCount { get; set; }
        public required string Slug { get; set; }
        public required string AvatarUrl { get; set; }
    }
}
