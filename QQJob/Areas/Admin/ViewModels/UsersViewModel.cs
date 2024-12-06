using QQJob.Models;

namespace QQJob.Areas.Admin.ViewModels
{
    public class UsersViewModel
    {
        public IQueryable<AppUser> Users { get; set; }
    }
}