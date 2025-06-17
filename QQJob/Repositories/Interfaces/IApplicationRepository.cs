using QQJob.Models;

namespace QQJob.Repositories.Interfaces
{
    public interface IApplicationRepository:IGenericRepository<Application>
    {
        Task<IEnumerable<Application>> GetApplications(int id,int page = 1,int per = 3);
        public Task<IEnumerable<Application>> GetApplicationsByEmployerId(string id,int per = 5);
    }
}
