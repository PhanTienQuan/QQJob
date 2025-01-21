using QQJob.Models;
using QQJob.Models.Enum;
using System.Linq.Expressions;

namespace QQJob.Repositories.Interfaces
{
    public interface IJobRepository:IGenericRepository<Job>
    {
        Task<IEnumerable<Job>> GetJobsByEmployerIdAsync(string employerId);
        Task<IEnumerable<Job>> SearchJobsAsync(string keyword);
        Task<Job> GetByIdAsync(int id);
        Task<IEnumerable<Job>> FindJobs(string query);
        Task<(IEnumerable<Job> jobs, PagingModel pagingModel)> GetJobsAsync(int currentPage,int pageSize,Expression<Func<Job,bool>>? predicate,string? searchValue = null,Status? searchStatus = null,DateTime? fromDate = null,DateTime? toDate = null);
    }
}
