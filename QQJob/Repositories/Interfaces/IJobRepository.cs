using QQJob.Dtos;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.ViewModels;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace QQJob.Repositories.Interfaces
{
    public interface IJobRepository : IGenericRepository<Job>
    {
        Task<Job?> GetJobDetail(Expression<Func<Job, bool>>? predicate);
        Task<IEnumerable<Job>> GetJobsByEmployerIdAsync(string employerId);
        Task<IEnumerable<Job>> SearchJobsAsync(string keyword);
        Task<Job> GetByIdAsync(int id);
        Task<IEnumerable<Job>?> FindJobs(Expression<Func<Job, bool>>? predicate);
        Task<(IEnumerable<Job> jobs, PagingModel pagingModel)> GetJobsAsync(int currentPage, int pageSize, Expression<Func<Job, bool>>? predicate, string? searchValue = null, Status? searchStatus = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<Job>> GetJobsByIdsAsync(JobSearchIntent intent);
        Task<JobDetailViewModel?> GetJobDetailViewModelAsync(int jobId);
    }
}
