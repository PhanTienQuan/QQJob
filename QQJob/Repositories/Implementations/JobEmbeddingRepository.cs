using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class JobEmbeddingRepository(QQJobContext context):GenericRepository<JobEmbedding>(context), IJobEmbeddingRepository
    {
    }
}
