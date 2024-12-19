using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class AppUserRepository : GenericRepository<AppUser>, IAppUserRepository
    {
        public AppUserRepository(QQJobContext context) : base(context)
        {
        }

        public async Task<AppUser?> GetUserWithDetailsAsync(string userId)
        {
            return await _context.Set<AppUser>()
                .Include(u => u.Employer)
                .Include(u => u.Candidate)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<IEnumerable<AppUser>> GetPremiumUsersAsync()
        {
            return await _context.Set<AppUser>()
                .Where(u => u.IsPremium)
                .ToListAsync();
        }

        public async Task<IEnumerable<AppUser>> GetUsersAsync(int page, int per)
        {
            int skip = (page - 1) * per;
            return await _context.Set<AppUser>()
                                 .OrderBy(u => u.FullName)
                                 .Skip(skip)
                                 .Take(per)
                                 .ToListAsync();
        }
        public async Task<int> GetCount()
        {
            return await _context.Set<AppUser>().CountAsync();
        }
    }
}
