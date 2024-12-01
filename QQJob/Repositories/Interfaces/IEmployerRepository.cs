using QQJob.Models;

namespace QQJob.Repositories.Interfaces
{
    public interface IEmployerRepository : IGenericRepository<Employer>
    {
        Task<Employer?> GetEmployerWithJobsAsync(string employerId);
    }
}

