using QQJob.Models;
using QQJob.Models.Enum;
using System.Linq.Expressions;

namespace QQJob.Repositories.Interfaces
{
    public interface IApplicationRepository:IGenericRepository<Application>
    {
        Task<IEnumerable<Application>> GetApplications(int id,int page = 1,int per = 3);
        public Task<IEnumerable<Application>> GetApplicationsByEmployerId(string id,int per = 5);
        Task<(IEnumerable<Application> applications, PagingModel pagingModel)> GetApplicationsAsync(int currentPage,int pageSize,Expression<Func<Application,bool>>? predicate,string? searchValue = null,ApplicationStatus? searchStatus = null,DateTime? appliedDate = null);
    }
}
