using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Services
{
    public class CleanupBackgroundService(IServiceProvider serviceProvider):BackgroundService
    {
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using(var scope = serviceProvider.CreateScope())
                    {
                        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                        var appUserRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
                        await UserDeletion(appUserRepository,notificationRepository);
                        var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
                        await CloseJob(jobRepository,notificationRepository);
                    }
                }
                catch(Exception ex)
                {
                    // Log exception (add your logger here)
                }

                // Wait 24 hours before next run
                await Task.Delay(TimeSpan.FromHours(24),stoppingToken);
            }
        }
        public async Task UserDeletion(IAppUserRepository appUserRepository,INotificationRepository notificationRepository)
        {
            var user = await appUserRepository.FindAsync(u => u.MarkedForDeletionAt != null && u.MarkedForDeletionAt <= DateTime.Now);
            if(user != null)
            {
                foreach(var u in user)
                {

                    var notification = new Notification
                    {
                        Content = $"User '{u.Id} - {u.FullName}' has been delete by the system.",
                        CreatedDate = DateTime.Now,
                        Type = Models.Enum.NotificationType.JobClosed,
                        UserType = Models.Enum.UserType.Admin,
                    };
                    await notificationRepository.AddAsync(notification);
                    await notificationRepository.SaveChangesAsync();

                    // Delete user
                    appUserRepository.Delete(u);
                    await appUserRepository.SaveChangesAsync();
                }
            }
        }
        public async Task CloseJob(IJobRepository jobRepository,INotificationRepository notificationRepository)
        {
            var jobs = await jobRepository.FindAsync(j => j.CloseDate <= DateTime.Now);
            if(jobs != null)
            {
                foreach(var job in jobs)
                {
                    // Close job
                    job.Status = Models.Enum.Status.Closed;
                    jobRepository.Update(job);
                    await jobRepository.SaveChangesAsync();

                    var notification = new Notification
                    {
                        Content = $"Job '{job.JobTitle}' has been closed by the system.",
                        CreatedDate = DateTime.Now,
                        ReceiverId = job.EmployerId,
                        Type = Models.Enum.NotificationType.JobClosed,
                        UserType = Models.Enum.UserType.User,
                    };
                    await notificationRepository.AddAsync(notification);
                    await notificationRepository.SaveChangesAsync();
                }
            }
        }
    }
}
