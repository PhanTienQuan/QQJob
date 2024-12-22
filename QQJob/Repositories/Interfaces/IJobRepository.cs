using QQJob.Models;

namespace QQJob.Repositories.Interfaces
{
    public interface IJobRepository : IGenericRepository<Job>
    {
        Task<IEnumerable<Job>> GetJobsByEmployerIdAsync(string employerId);
        Task<IEnumerable<Job>> SearchJobsAsync(string keyword);
        Task<IEnumerable<Job>> GetJobsAsync(int page = 1, int pageSize = 10);
        Task<Job> GetByIdAsync(int id);
    }
}
