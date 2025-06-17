using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QQJob.Repositories.Implementations
{
    public class AppUserRepository:GenericRepository<AppUser>, IAppUserRepository
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

        public async Task<IEnumerable<AppUser>> GetUsersAsync(int page,int per)
        {
            int skip = (page - 1) * per;
            return await _context.Set<AppUser>()
                                 .OrderBy(u => u.FullName)
                                 .Skip(skip)
                                 .Take(per)
                                 .ToListAsync();
        }

        public async Task<IEnumerable<AppUser>> GetUserAsync(Expression<Func<AppUser,bool>> predicate = null)
        {

            // Build the query
            IQueryable<AppUser> query = _context.Set<AppUser>();

            // Apply the predicate if it's provided
            if(predicate != null)
            {
                query = query.Where(predicate);
            }

            // Execute the query
            return await query.ToListAsync();
        }
        public async Task<int> GetCount()
        {
            return await _context.Set<AppUser>().CountAsync();
        }

        public string GetUserAvatarUrl(string userId)
        {
            var user = _context.Set<AppUser>().Where(user => user.Id == userId).FirstOrDefault();
            return user.Avatar;
        }
    }
}
