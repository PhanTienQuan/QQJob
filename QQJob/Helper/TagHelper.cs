using QQJob.Repositories.Interfaces;

namespace QQJob.Helper
{
    public static class TagHelper
    {
        private static IServiceProvider _serviceProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public static async Task<string> GetFullNameAsync(string username)
        {
            if(_serviceProvider == null)
            {
                throw new InvalidOperationException("TagHelper is not initialized. Call Initialize() first.");
            }

            using(var scope = _serviceProvider.CreateScope())
            {
                var appUserRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
                var users = await appUserRepository.FindAsync(u => u.UserName == username);
                var user = users.FirstOrDefault();

                return user?.FullName ?? "User not found";
            }
        }
    }
}
