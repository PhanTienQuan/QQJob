using QQJob.Models.Enum;

namespace QQJob.Dtos
{
    public class JobDto
    {
        public string? Title { get; set; }
        public DateTime PostDate { get; set; }
        public Status Status { get; set; }
        public ICollection<SkillDto>? Skills { get; set; }
        public int OpenPosition { get; set; }
        public string Slug { get; set; }
        public EmployerDto Employer { get; set; }
    }
}
