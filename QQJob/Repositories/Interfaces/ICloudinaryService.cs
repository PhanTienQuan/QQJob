namespace QQJob.Repositories.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string> UploadEvidentAsync(IFormFile file,string userId);
        Task<string> UpdateAvatar(IFormFile file,string userId);
        public Task<bool> DeleteFile(string publicId);
    }
}
