using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class NotificationRepository:GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(QQJobContext context) : base(context)
        {
        }

        public async Task<int> GetUploadAttemptToday(string userId)
        {
            var today = DateTime.Now.Date;

            return await _dbSet.Where(n => n.ReceiverId == userId && n.CreatedDate >= today && n.Type == NotificationType.EvidenceUploaded).CountAsync();
        }
        public async Task<List<Notification>> GetUserNotificationsAsync(string userId,int take)
        {
            return await _dbSet
                .Where(n => n.ReceiverId == userId)
                .OrderByDescending(n => n.Id)
                .Take(take)
                .ToListAsync();
        }
        public async Task<int> GetUserUnreadNotificationCountAsync(string userId)
        {
            return await _dbSet
                .Where(n => n.ReceiverId == userId && !n.IsReaded)
                .CountAsync();
        }
    }
}
