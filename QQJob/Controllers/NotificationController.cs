using Microsoft.AspNetCore.Mvc;
using QQJob.Repositories.Interfaces;

namespace QQJob.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class NotificationController:Controller
    {
        private readonly INotificationRepository _notificationRepository;
        public NotificationController(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if(notification == null)
                return NotFound();

            notification.IsReaded = true;
            _notificationRepository.Update(notification);
            await _notificationRepository.SaveChangesAsync();
            return Ok();
        }
    }
}
