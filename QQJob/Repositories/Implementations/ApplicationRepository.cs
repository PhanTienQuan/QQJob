using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QQJob.Repositories.Implementations
{
    public class ApplicationRepository(QQJobContext context):GenericRepository<Application>(context), IApplicationRepository
    {
        public async Task<IEnumerable<Application>> GetApplications(int id,int page,int per)
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

        public async Task<IEnumerable<Application>> GetApplicationsByEmployerId(string id,int per)
        {
            return await _context.Applications
            .Where(a => a.Job.EmployerId == id)
            .OrderByDescending(a => a.ApplicationDate)
            .Take(per)
            .Include(a => a.Job)
            .Include(a => a.Candidate)
            .ThenInclude(c => c.User)
            .ToListAsync();
        }
        public async Task<(IEnumerable<Application> applications, PagingModel pagingModel)> GetApplicationsAsync(int currentPage,int pageSize,Expression<Func<Application,bool>>? predicate,string? searchValue = null,ApplicationStatus? searchStatus = null,DateTime? appliedDate = null)
        {
            var query = _context.Applications.AsQueryable();

            if(predicate != null)
            {
                query = query.Where(predicate);
            }

            if(!string.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToLower();
                query = query.Where(a => a.Candidate.User.FullName.ToLower().Contains(searchValue) ||
                                         a.Job.JobTitle.ToLower().Contains(searchValue)
                );
            }

            if(searchStatus.HasValue)
            {
                query = query.Where(a =>
                    a.Status == searchStatus
                );
            }

            if(appliedDate.HasValue)
            {
                query = query.Where(a => a.ApplicationDate >= appliedDate);
            }

            var totalItems = await query.CountAsync();

            var applications = await query
                .OrderByDescending(j => j.ApplicationDate)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .Include(a => a.Candidate)
                .ThenInclude(c => c.User)
                .Include(a => a.Candidate.Resume)
                .Include(a => a.Job)
                .ThenInclude(j => j.Employer)
                .ToListAsync();

            var pagingModel = new PagingModel
            {
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return (applications, pagingModel);
        }
        public async Task<Application?> GetApplicationById(int id)
        {
            return await _dbSet
                .Where(a => a.ApplicationId == id)
                .Include(a => a.Candidate)
                .ThenInclude(c => c.Resume)
                .Include(a => a.Candidate.User)
                .Include(a => a.Job)
                .ThenInclude(j => j.Employer)
                .ThenInclude(e => e.User)
                .FirstOrDefaultAsync();
        }

        public void UpdateAppplicantRank(int jobId,string candidateId,float rank)
        {
            var application = _dbSet.FirstOrDefault(a => a.JobId == jobId && a.CandidateId == candidateId);
            if(application != null)
            {
                application.AIRanking = rank;
                _context.SaveChanges();
            }
        }
    }
}
