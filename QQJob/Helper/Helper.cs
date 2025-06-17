using Humanizer;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
namespace QQJob.Helper
{
    public static class Helper
    {
        private static IServiceProvider? _serviceProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        public static async Task<string> GetFullNameAsync(string? username)
        {
            using(var scope = _serviceProvider.CreateScope())
            {
                var appUserRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
                var users = await appUserRepository.FindAsync(u => u.UserName == username);
                var user = users.FirstOrDefault();

                return user?.FullName ?? "User not found";
            }
        }
        public static async Task<string?> GetUserRole(string? username)
        {
            using(var scope = _serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var user = await userManager.FindByNameAsync(username);
                var role = await userManager.GetRolesAsync(user);

                return role.Any() ? role.FirstOrDefault().ToString() : null;
            }
        }
        public static async Task<IHtmlContent> GetUserHomePageAction(string? username,IUrlHelper urlHelper)
        {
            using(var scope = _serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var user = await userManager.FindByNameAsync(username);
                var role = await userManager.GetRolesAsync(user);
                if(role.Any())
                {
                    if(role.FirstOrDefault().ToString() == "Candidate")
                    {
                        return new HtmlString($@"<a href=""{urlHelper.Action("Resume","Candidate")}""
                                        class=""small__btn d-none d-sm-flex d-xl-flex fill__btn border-6 font-xs""
                                        aria-label=""Resume Button"">
                                        Manage CV
                                    </a>");
                    }
                    else
                    {
                        return new HtmlString($@"<a href=""{urlHelper.Action("PostJob","Employer")}"" 
                                        class=""small__btn d-none d-sm-flex d-xl-flex fill__btn border-6 font-xs""
                                        aria-label=""Job Posting Button"">
                                        Add A Job
                                    </a>");
                    }
                }
                else
                {
                    return new HtmlString($@"<a href=""#"" onclick=""showSetAccountTypeModel()""
                                        class=""small__btn d-none d-sm-flex d-xl-flex fill__btn border-6 font-xs""
                                        aria-label=""Job Posting Button"">
                                        Set Up Account Type
                                    </a>");
                }
            }
        }
        public static async Task<string> GetUserAvatarUrlAsync(string? username)
        {
            using(var scope = _serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var user = await userManager.FindByNameAsync(username);

                return user.Avatar ?? "~/assets/img/avatars/default-avatar.jpg";
            }
        }
        //no done
        public static IHtmlContent DisplayUserStatus(UserStatus status)
        {
            return new HtmlString($@" <input value=""{status}"" class=""text-success"" disabled />");
        }
        public static string DisplayLimitedText(string address,int maxLength = 10)
        {
            if(string.IsNullOrEmpty(address)) return address;
            return address.Length <= maxLength ? address : address.Substring(0,maxLength) + "...";
        }
        public static string DisplayTime(DateTime time)
        {
            var result = time.ToUniversalTime().Humanize().Replace("from now","ago");

            return char.ToUpper(result[0]) + result.Substring(1);
        }
        public static string ShortenURL(string url)
        {
            string trimmed = url.Replace("https://","").Replace("http://","");

            string result = char.ToUpper(trimmed[0]) + trimmed.Substring(1);
            return result;
        }
    }
}
