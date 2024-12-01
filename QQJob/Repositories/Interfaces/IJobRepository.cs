using QQJob.Models;

namespace QQJob.Repositories.Interfaces
{
    public interface IJobRepository : IGenericRepository<Job>
    {
        Task<IEnumerable<Job>> GetJobsByEmployerIdAsync(string employerId);
        Task<IEnumerable<Job>> SearchJobsAsync(string keyword);
    }
}
