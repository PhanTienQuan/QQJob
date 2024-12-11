using Microsoft.AspNetCore.Mvc;
using QQJob.Data.CValidation;
using System.ComponentModel.DataAnnotations;

namespace QQJob.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [HiddenInput]
        public bool AccountType { get; set; } = true;

        [EmailAddress]
        [Required(ErrorMessage = "Email is required")]
        [Display(Name = "Your Email", Prompt = "Enter your Email")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password", Prompt = "Enter password")]
        [Password]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password", Prompt = "Confirm your password")]
        [ConfirmPassword("Password")]
        public string? ConfirmPassword { get; set; }
    }
}
