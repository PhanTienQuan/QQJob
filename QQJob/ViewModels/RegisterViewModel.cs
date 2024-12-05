using Microsoft.AspNetCore.Mvc;
using QQJob.Data.CValidation;
using System.ComponentModel.DataAnnotations;

namespace QQJob.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Your Username", Prompt = "Your Username")]
        public string UserName { get; set; }

        [Required]
        [HiddenInput]
        public bool AccountType { get; set; } = true;

        [EmailAddress]
        [Required(ErrorMessage = "Email is required")]
        [Display(Name = "Your Email", Prompt = "Your Email")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password", Prompt = "Password")]
        [Password]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password", Prompt = "Confirm password")]
        [ConfirmPassword("Password")]
        public string? ConfirmPassword { get; set; }
    }
}
