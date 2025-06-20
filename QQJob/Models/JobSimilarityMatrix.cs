using Microsoft.EntityFrameworkCore;

namespace QQJob.Models
{
    [PrimaryKey("JobId1","JobId2")]
    public class JobSimilarityMatrix
    {
        public int JobId1 { get; set; }

        public int JobId2 { get; set; }

        public double SimilarityScore { get; set; }
    }
}
