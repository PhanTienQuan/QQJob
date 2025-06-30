using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class CloudinaryService:ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        public CloudinaryService(IConfiguration configuration)
        {
            var cloudinary = configuration.GetSection("Cloudinary");
            var cloudinaryConfig = new Account(
                cloudinary["name"],
                cloudinary["key"],
                cloudinary["secret"]
                );

            if(string.IsNullOrEmpty(cloudinaryConfig.Cloud) || string.IsNullOrEmpty(cloudinaryConfig.ApiKey) || string.IsNullOrEmpty(cloudinaryConfig.ApiSecret))
            {
                throw new ArgumentException("Cloudinary configuration is missing or incomplete");
            }
            _cloudinary = new Cloudinary(cloudinaryConfig);
        }

        public async Task<string> UploadEvidentAsync(IFormFile file,string userId)
        {
            if(file == null || file.Length == 0)
            {
                return "Invalid file! Try again.";
            }

            var uploadParams = new RawUploadParams()
            {
                File = new FileDescription(file.FileName,file.OpenReadStream()),
                AssetFolder = "company_evidents",
                PublicId = $"company_{userId}_evident",
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if(uploadResult == null || uploadResult.SecureUrl == null)
            {
                throw new Exception($"Upload to Cloudinary failed: {uploadResult?.Error.Message}");
            }

            return uploadResult.SecureUrl.AbsoluteUri;
        }

        public async Task<bool> DeleteFile(string publicUrl)
        {
            try
            {
                var publicId = publicUrl.Split("/").ToList().Last();
                var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Raw
                });
                return result.Result == "ok";
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<string> UpdateAvatar(IFormFile file,string userId)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName,file.OpenReadStream()),
                AssetFolder = "user_avatars",
                PublicId = $"user_{userId}_avatar",
                Overwrite = true,
                EagerTransforms = new List<Transformation> {
                        new Transformation().Width(320).Height(320).Crop("fill").Gravity("face").FetchFormat("png").Radius("max")
                    },
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if(uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return uploadResult.Eager.FirstOrDefault().SecureUrl.AbsoluteUri;
            }
            else
            {
                throw new Exception($"Upload to Cloudinary failed: {uploadResult.Error.Message}");
            }
        }

        public async Task<string> UploadResumeAsync(IFormFile file,string userId)
        {
            if(file == null || file.Length == 0)
            {
                return "Invalid file! Try again.";
            }

            var uploadParams = new RawUploadParams()
            {
                File = new FileDescription(file.FileName,file.OpenReadStream()),
                AssetFolder = "candidate_resumes",
                PublicId = $"{userId}_resume_{Guid.NewGuid}",
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if(uploadResult == null || uploadResult.SecureUrl == null)
            {
                throw new Exception($"Upload to Cloudinary failed: {uploadResult?.Error?.Message}");
            }

            return uploadResult.SecureUrl.AbsoluteUri;
        }
    }
}
