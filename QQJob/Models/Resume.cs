using System.ComponentModel.DataAnnotations;

namespace QQJob.Models
{
    public class Resume
    {
        [Key]
        public Guid ResumeId { get; set; }
        public required string CandidateId { get; set; }
        public string? Url { get; set; }
        public string? AiSumary { get; set; }
        public string? Embedding { get; set; }
    }
}
