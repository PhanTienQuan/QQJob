using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QQJob.Repositories.Implementations
{
    public class JobRepository:GenericRepository<Job>, IJobRepository
    {
        public JobRepository(QQJobContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<Job> jobs, PagingModel pagingModel)> GetJobsAsync(int currentPage,int pageSize,Expression<Func<Job,bool>>? predicate = null,string? searchValue = null,Status? searchStatus = null,DateTime? fromDate = null,DateTime? toDate = null)
        {
            var query = _context.Jobs.AsQueryable();

            if(predicate != null)
            {
                query = query.Where(predicate);
            }

            if(!string.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToLower();
                query = query.Where(j =>
                    j.Title.ToLower().Contains(searchValue) ||
                    j.Address.ToLower().Contains(searchValue) ||
                    (j.JobDescription != null && j.JobDescription.ToLower().Contains(searchValue))
                );
            }

            if(searchStatus.HasValue)
            {
                query = query.Where(j =>
                    j.Status == searchStatus
                );
            }

            if(fromDate.HasValue)
            {
                query = query.Where(j => j.PostDate >= fromDate);
            }

            if(toDate.HasValue)
            {
                query = query.Where(j => j.CloseDate <= toDate.Value.AddDays(1).AddTicks(-1));
            }

            var totalItems = await query.CountAsync();

            var jobs = await query
                .OrderByDescending(j => j.PostDate)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagingModel = new PagingModel
            {
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return (jobs, pagingModel);
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
        public async Task<Job> GetByIdAsync(int id)
        {
            return await _context.Set<Job>().Include(j => j.Skills).Include(j => j.Applications).FirstOrDefaultAsync(j => j.JobId == id);
        }

        public async Task<IEnumerable<Job>?> FindJobs(Expression<Func<Job,bool>>? predicate = null)
        {
            return predicate == null
                ? null
                : await _context.Jobs.Where(predicate).Include(job => job.Skills).ToListAsync();
        }

        public async Task<Job?> GetJobDetail(Expression<Func<Job,bool>>? predicate = null)
        {
            return predicate == null
                ? null
                : await _context.Jobs.FirstOrDefaultAsync(predicate);
        }
    }
}
