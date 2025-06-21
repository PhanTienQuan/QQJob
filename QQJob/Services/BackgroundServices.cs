
namespace QQJob.Services
{
    public class BackgroundServices:BackgroundService
    {
        private readonly string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(),"wwwroot","uploads");
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            CleanupOldTempFiles();
            await Task.Delay(TimeSpan.FromMinutes(10),stoppingToken);
        }

        private void CleanupOldTempFiles()
        {
            if(!Directory.Exists(uploadsFolder))
                return;

            var files = Directory.GetFiles(uploadsFolder);

            foreach(var file in files)
            {
                try
                {
                    var creationTime = File.GetCreationTimeUtc(file);

                    if(creationTime < DateTime.UtcNow.AddMinutes(-30))
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted old temp file: {file}");
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error deleting temp file: {file} - {ex.Message}");
                }
            }
        }
    }
}
