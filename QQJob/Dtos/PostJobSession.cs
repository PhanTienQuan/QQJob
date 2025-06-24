namespace QQJob.Dtos
{
    public class PostJobSession
    {
        public string? JobTitle { get; set; }
        public string? Description { get; set; }
        public string? City { get; set; }
        public string? ExperienceLevel { get; set; }
        public string? Salary { get; set; }
        public string? SalaryType { get; set; }
        public string? JobType { get; set; }
        public string? Skills { get; set; }
        public int? Opening { get; set; }
        public string? LocationRequirement { get; set; }
        public DateTime? CloseDate { get; set; }
    }

}
