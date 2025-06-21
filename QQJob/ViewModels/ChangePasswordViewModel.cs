using System.ComponentModel.DataAnnotations;

namespace QQJob.ViewModels
{
    public class ChangePasswordViewModel
    {
        public bool HasPassword { get; set; } = false;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password,ErrorMessage = "Invalid password format.")]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New Password is required.")]
        [DataType(DataType.Password,ErrorMessage = "Invalid password format.")]
        [Display(Name = "New Password")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password,ErrorMessage = "Invalid password format.")]
        [Display(Name = "Confirm New Password")]
        [Compare("Password",ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
