using System.ComponentModel.DataAnnotations;

namespace QQJob.Areas.Admin.ViewModels
{
    public class EditRoleViewModel
    {
        [Required]
        public string Id { get; set; }

        [Required(ErrorMessage = "Role Name is Required")]
        public string RoleName { get; set; }
    }
}
