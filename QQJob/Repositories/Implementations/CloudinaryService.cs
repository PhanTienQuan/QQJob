using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        public CloudinaryService(IConfiguration configuration)
        {
            var cloudinary = configuration.GetSection("Cloudinary");
            var cloudinaryConfig = new Account(
                cloudinary["CloudName"],
                cloudinary["ApiKey"],
                cloudinary["ApiSecret"]
                );

            if(string.IsNullOrEmpty(cloudinaryConfig.Cloud) || string.IsNullOrEmpty(cloudinaryConfig.ApiKey) || string.IsNullOrEmpty(cloudinaryConfig.ApiSecret))
            {
                throw new ArgumentException("Cloudinary configuration is missing or incomplete");
            }
            _cloudinary = new Cloudinary(cloudinaryConfig);
        }

        public async Task<string> UploadEvidentAsync(IFormFile file)
        {
            if(file == null || file.Length == 0)
            {
                return "Invalid file";
            }

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                EagerTransforms = new List<Transformation> {
                        new Transformation().Width(250).Height(250).Crop("fill").Gravity("auto").FetchFormat("jpg")
                    },
                AssetFolder = "Evident"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if(uploadResult == null || uploadResult.SecureUrl == null)
            {
                throw new Exception($"Upload to Cloudinary failed: {uploadResult.Error.Message}");
            }

            var url = uploadResult.Eager?.FirstOrDefault()?.SecureUrl.AbsoluteUri;
            if(!string.IsNullOrEmpty(url))
            {
                return url;
            }

            return uploadResult.SecureUrl.AbsoluteUri;
        }

    }
}
