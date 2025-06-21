using System.ComponentModel.DataAnnotations;

namespace QQJob.ViewModels
{
    public class DeleteProfileViewModel
    {
        public bool HasPassword { get; set; } = false;
        public DateTime? DeteleAt { get; set; }
        [Required(ErrorMessage = "Please enter your password to confirm deletion.")]
        [DataType(DataType.Password,ErrorMessage = "Invalid password format.")]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
}
