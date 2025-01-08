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

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full name",Prompt = "Enter your full name")]
        public string Fullname { get; set; }

        [EmailAddress]
        [Required(ErrorMessage = "Email is required")]
        [Display(Name = "Your email",Prompt = "Enter your Email")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password",Prompt = "Enter password")]
        [Password]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password",Prompt = "Confirm your password")]
        [ConfirmPassword("Password")]
        public string? ConfirmPassword { get; set; }
    }
}
