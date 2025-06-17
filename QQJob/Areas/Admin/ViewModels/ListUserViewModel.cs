using QQJob.Models;
using System.ComponentModel.DataAnnotations;

namespace QQJob.Areas.Admin.ViewModels
{
    public class ListUserViewModel
    {
        public AppUser User { get; set; }
        [Display(Name = "Role")]
        public string Role { get; set; }
    }
}
