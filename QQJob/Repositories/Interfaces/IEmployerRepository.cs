using QQJob.Models;

namespace QQJob.Repositories.Interfaces
{
    public interface IEmployerRepository:IGenericRepository<Employer>
    {
        Task<Employer?> GetEmployerWithJobsAsync(string employerId);
        Task<IEnumerable<Employer?>> GetAllRQEmployerAsync(int page = 1,int per = 10);
        Task<IEnumerable<string>> GetAllEmployerIdAsync();
        Task<Employer?> GetEmployerByName(string username);
        Task<Employer> GetByIdAsync(string id);
        Task<Employer?> GetBySlugAsync(string slug);
    }
}

