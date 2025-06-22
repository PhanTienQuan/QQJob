using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QQJob.Repositories.Implementations
{
    public class EmployerRepository:GenericRepository<Employer>, IEmployerRepository
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

        public async Task<IEnumerable<Employer?>> GetAllRQEmployerAsync(int page,int per)
        {
            int skip = (page - 1) * per;
            return await _context.Set<Employer>()
                    .Include(e => e.User)
                    .Where(e => e.User.IsVerified == UserStatus.Pending)
                    .OrderBy(e => e.User.FullName)
                    .AsNoTracking()
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
                .AsSplitQuery()  // This will split the query into separate ones for better performance
                .FirstOrDefaultAsync();
        }
        public async Task<Employer?> GetByIdAsync(string? id)
        {
            return await _context.Set<Employer>()
                .Include(e => e.User)
                .Include(e => e.Jobs)
                .ThenInclude(j => j.Applications)
                .Include(e => e.Jobs)
                    .ThenInclude(j => j.Skills)
                .Include(e => e.Follows)
                .Include(e => e.CompanyEvident)
                .Where(e => e.EmployerId == id)
                .FirstOrDefaultAsync();
        }
        public async Task<Employer?> GetBySlugAsync(string slug)
        {
            return await _context.Employers
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.User.Slug == slug);
        }

        public async Task<IEnumerable<Employer>> GetAllWithDetailAsync()
        {
            return await _dbSet
                .Include(e => e.User)
                .Include(e => e.Jobs)
                .ThenInclude(j => j.Applications)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(IEnumerable<Employer> employers, PagingModel pagingModel)> GetJobsAsync(int currentPage,int pageSize,string? employerName = null,string? field = null,DateTime? foundDate = null,Expression<Func<Employer,bool>>? predicate = null)
        {
            var query = _dbSet.AsQueryable();

            if(predicate != null)
            {
                query = query.Where(predicate);
            }

            if(!string.IsNullOrEmpty(employerName))
            {
                employerName = employerName.ToLower();
                query = query.Where(e =>
                    e.User.FullName != null && e.User.FullName.ToLower().Contains(employerName)
                );
            }

            if(!string.IsNullOrEmpty(field))
            {
                field = field.ToLower();
                query = query.Where(e =>
                    !string.IsNullOrEmpty(e.CompanyField) &&
                    (e.CompanyField.Contains(field) || field.Contains(e.CompanyField))
                );
            }

            if(foundDate.HasValue)
            {
                query = query.Where(j => j.FoundedDate == foundDate.Value);
            }

            var totalItems = await query.CountAsync();

            var employers = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .Include(e => e.User)
                .Include(e => e.Jobs)
                .ToListAsync();

            var pagingModel = new PagingModel
            {
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return (employers, pagingModel);
        }
    }
}