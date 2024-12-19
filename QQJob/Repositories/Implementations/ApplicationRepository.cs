using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class ApplicationRepository(QQJobContext context) : GenericRepository<Application>(context), IApplicationRepository
    {
        public async Task<IEnumerable<Application>> GetApplications(int id, int page, int per)
        {
            int skip = (page - 1) * per;
            return await _context.Set<Application>()
                .Where(a => a.JobId == id)
                .OrderBy(a => a.ApplicationDate)
                .Skip(skip)
                .Take(per)
                .Include(a => a.Candidate)
                .ToListAsync();
        }
    }
}
