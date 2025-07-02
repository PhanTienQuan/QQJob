using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QQJob.Repositories.Implementations
{
    public class FollowRepository(QQJobContext context):GenericRepository<Follow>(context), IFollowRepository
    {

        public async Task<(List<Follow> follows, PagingModel pagingModel)> GetFollowsAsync(int currentPage,int pageSize,Expression<Func<Follow,bool>>? predicate)
        {
            var query = _dbSet.AsQueryable();

            if(predicate != null)
            {
                query = query.Where(predicate);
            }
            var totalItems = await query.CountAsync();

            var follows = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .Include(f => f.Employer)
                .ThenInclude(e => e.User)
                .Include(f => f.Employer.Jobs)
                .ToListAsync();

            var pagingModel = new PagingModel
            {
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return (follows, pagingModel);
        }

        public async Task<bool> IsFollowedAsync(string employerId,string candidateId)
        {
            return await _dbSet.AnyAsync(f => f.EmployerId == employerId && f.CandidateId == candidateId);
        }

    }
}
