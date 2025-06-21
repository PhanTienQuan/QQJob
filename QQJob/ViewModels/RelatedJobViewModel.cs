namespace QQJob.ViewModels
{
    public class RelatedJobViewModel
    {
        public int Id { get; set; }
        public string? Avatar { get; set; }
        public string? City { get; set; }
        public string? JobType { get; set; }
        public string? Title { get; set; }
        public string? Slug { get; set; }
        public int? Opening { get; set; }
        public IEnumerable<string> Skills { get; set; } = new List<string>();
    }
}
