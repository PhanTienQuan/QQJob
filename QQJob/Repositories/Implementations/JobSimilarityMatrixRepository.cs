using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class JobSimilarityMatrixRepository(QQJobContext context):GenericRepository<JobSimilarityMatrix>(context), IJobSimilarityMatrixRepository
    {
        public async Task<IEnumerable<int>> GetRelatedJobIdsAsync(int jobId,int topN = 5)
        {
            return await _dbSet
                .Where(x => x.JobId1 == jobId)
                .OrderByDescending(x => x.SimilarityScore)
                .Take(topN)
                .Select(x => x.JobId2)
                .ToListAsync();
        }
    }
}
