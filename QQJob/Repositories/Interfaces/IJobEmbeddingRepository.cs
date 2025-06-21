using QQJob.Models;

namespace QQJob.Repositories.Interfaces
{
    public interface IJobEmbeddingRepository:IGenericRepository<JobEmbedding>
    {
        Task AddOrUpdateAsync(JobEmbedding embedding);
    }
}
