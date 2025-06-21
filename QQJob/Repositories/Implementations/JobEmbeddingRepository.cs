using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class JobEmbeddingRepository(QQJobContext context):GenericRepository<JobEmbedding>(context), IJobEmbeddingRepository
    {
        public async Task AddOrUpdateAsync(JobEmbedding embedding)
        {
            var existing = await _dbSet
                .FirstOrDefaultAsync(e => e.JobId == embedding.JobId);

            if(existing != null)
            {
                existing.Embedding = embedding.Embedding;
                existing.UpdatedAt = embedding.UpdatedAt;
                _dbSet.Update(existing);
            }
            else
            {
                await _dbSet.AddAsync(embedding);
            }

            await _context.SaveChangesAsync();
        }

    }
}
