using System.ComponentModel.DataAnnotations;

namespace QQJob.Models
{
    public class JobEmbedding
    {
        [Key]
        public int EmbeddingId { get; set; }
        public required int JobId { get; set; }
        public required string Embedding { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
