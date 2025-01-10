using Microsoft.AspNetCore.Authentication;
using QQJob.Data.CValidation;
using System.ComponentModel.DataAnnotations;

namespace QQJob.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Wrong email format.")]
        [Display(Name = "Your Email",Prompt = "Enter your Email")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password",Prompt = "Enter password")]
        [Password]
        public string? Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
        public IList<AuthenticationScheme>? ExternalLogins { get; set; }
    }
}
