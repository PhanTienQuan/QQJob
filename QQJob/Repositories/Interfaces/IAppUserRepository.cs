using QQJob.Models;

namespace QQJob.Repositories.Interfaces
{
    public interface IAppUserRepository : IGenericRepository<AppUser>
    {
        Task<AppUser?> GetUserWithDetailsAsync(string userId);
        Task<IEnumerable<AppUser>> GetPremiumUsersAsync();
        Task<IEnumerable<AppUser>> GetUsersAsync(int page = 1, int per = 10);
        Task<int> GetCount();
    }
}
