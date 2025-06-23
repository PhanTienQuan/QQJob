namespace QQJob.Dtos
{
    public class ChatBoxSearchIntent
    {
        public string? IntentType { get; set; }
        public string? JobTitle { get; set; }
        public string? EmployerName { get; set; }
        public string? City { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public List<string> IncludeSkills { get; set; } = [];
        public List<string> ExcludeSkills { get; set; } = [];
        public string? JobType { get; set; }
        public string? ExperienceLevel { get; set; }
        public bool StrictSearch { get; set; } = false;
        public required string OriginalQuery { get; set; }
        public int TopN { get; set; }
        public List<string>? DescriptionKeywords { get; set; }

    }
}
