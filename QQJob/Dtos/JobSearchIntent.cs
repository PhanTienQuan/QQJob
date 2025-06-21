namespace QQJob.Dtos
{
    public class JobSearchIntent
    {
        public string? JobTitle { get; set; }
        public string? City { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public List<string> IncludeSkills { get; set; } = [];
        public List<string> ExcludeSkills { get; set; } = [];
        public string? JobType { get; set; }  // Fulltime, Part-time
        public string? ExperienceLevel { get; set; } // Junior, Mid, Senior
    }

}
