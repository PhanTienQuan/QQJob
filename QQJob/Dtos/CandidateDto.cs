namespace QQJob.Dtos
{
    public class CandidateDto
    {
        public UserDto User { get; set; }
        public string? WorkingType { get; set; }
        public ICollection<SkillDto> Skills { get; set; }
    }
}
