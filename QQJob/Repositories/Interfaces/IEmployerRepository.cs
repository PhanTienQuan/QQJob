using QQJob.Models;
using QQJob.Models.Enum;
using System.Linq.Expressions;

namespace QQJob.Repositories.Interfaces
{
    public interface IEmployerRepository:IGenericRepository<Employer>
    {
        Task<Employer?> GetEmployerWithJobsAsync(string employerId);
        Task<IEnumerable<Employer?>> GetAllRQEmployerAsync(int page = 1,int per = 10);
        Task<IEnumerable<string>> GetAllEmployerIdAsync();
        Task<Employer?> GetEmployerByName(string username);
        Task<Employer?> GetByIdAsync(string? id);
        Task<Employer?> GetBySlugAsync(string slug);
        Task<IEnumerable<Employer>> GetAllWithDetailAsync();
        Task<(IEnumerable<Employer> employers, PagingModel pagingModel)> GetJobsAsync(int currentPage,int pageSize,string? employerName = null,string? field = null,DateTime? foundDate = null,Expression<Func<Employer,bool>>? predicate = null);
    }
}

