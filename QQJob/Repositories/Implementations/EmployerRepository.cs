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

        public async Task<IEnumerable<Employer?>> GetAllRQEmployerAsync(int page, int per)
        {
            int skip = (page - 1) * per;
            return await _context.Set<Employer>()
                    .Include(e => e.User)
                    .Where(e => e.User.IsVerified == 0)
                    .OrderBy(e => e.User.FullName)
                    .AsNoTracking() // Avoids tracking entities
                    .Skip(skip)
                    .Take(per)
                    .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetAllEmployerIdAsync()
        {
            return await _context.Set<Employer>()
                            .Select(e => e.EmployerId)
                            .ToListAsync();
        }

        public async Task<Employer?> GetEmployerByName(string name)
        {
            return await _context.Set<Employer>()
                .Where(e => e.User.UserName == name)
                .Include(e => e.User)
                .Include(e => e.Jobs)
                .ThenInclude(j => j.Applications)
                .Include(e => e.Follows)
                .FirstOrDefaultAsync();
        }
    }
}