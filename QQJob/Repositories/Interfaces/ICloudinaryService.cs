namespace QQJob.Repositories.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string> UploadEvidentAsync(IFormFile file);
    }
}
