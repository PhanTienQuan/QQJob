using QQJob.Models;

namespace QQJob.Repositories.Interfaces
{
    public interface IAppUserRepository : IGenericRepository<AppUser>
    {
        Task<AppUser?> GetUserWithDetailsAsync(string userId);
        Task<IEnumerable<AppUser>> GetPremiumUsersAsync();
    }
}
