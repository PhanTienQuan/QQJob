namespace QQJob.Repositories.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string> UploadEvidentAsync(IFormFile file);
        Task<string> UpdateAvatar(IFormFile file,string userId);
    }
}
