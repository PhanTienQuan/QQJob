using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class JobRepository : GenericRepository<Job>, IJobRepository
    {
        public JobRepository(QQJobContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Job>> GetJobsAsync(int page = 1, int pageSize = 10)
        {
            int skip = (page - 1) * pageSize;
            return await _context.Set<Job>()
                .OrderBy(j => j.Title)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Job>> GetJobsByEmployerIdAsync(string employerId)
        {
            return await _context.Set<Job>()
                .Where(j => j.EmployerId == employerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Job>> SearchJobsAsync(string keyword)
        {
            return await _context.Set<Job>()
                .Where(j => j.Title.Contains(keyword) || j.JobDescription.Contains(keyword))
                .ToListAsync();
        }

    }
}
