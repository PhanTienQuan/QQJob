using System.ComponentModel.DataAnnotations;

namespace QQJob.Models
{
    public class JobEmbedding
    {
        [Key]
        public required int JobId { get; set; }
        public required string Embedding { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
