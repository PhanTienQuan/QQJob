using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class EmployerRepository : GenericRepository<Employer>, IEmployerRepository
    {
        public EmployerRepository(QQJobContext context) : base(context)
        {
        }

        public async Task<Employer?> GetEmployerWithJobsAsync(string employerId)
        {
            return await _context.Set<Employer>()
                .Include(e => e.Jobs)
                .FirstOrDefaultAsync(e => e.EmployerId == employerId);
        }

        public async Task<IEnumerable<Employer?>> GetAllEmployerAsync()
        {
            return await _context.Set<Employer>()
                .Include(e => e.User)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetAllEmployerIdAsync()
        {
            return await _context.Set<Employer>()
                            .Select(e => e.EmployerId)
                            .ToListAsync();
        }
    }
}