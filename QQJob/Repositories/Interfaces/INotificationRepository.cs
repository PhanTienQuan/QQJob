using QQJob.Models;

namespace QQJob.Repositories.Interfaces
{
    public interface INotificationRepository:IGenericRepository<Notification>
    {
        public Task<int> GetUploadAttemptToday(string userId);
        public Task<List<Notification>> GetUserNotificationsAsync(string userId,int take);
        public Task<int> GetUserUnreadNotificationCountAsync(string userId);
    }
}
