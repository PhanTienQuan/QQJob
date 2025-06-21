using QQJob.Models;

namespace QQJob.Repositories.Interfaces
{
    public interface IJobSimilarityMatrixRepository:IGenericRepository<JobSimilarityMatrix>
    {
        Task<IEnumerable<int>> GetRelatedJobIdsAsync(int jobId,int topN = 5);
    }
}
