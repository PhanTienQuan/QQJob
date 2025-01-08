using System.ComponentModel.DataAnnotations;

namespace QQJob.ViewModels
{
    public class ConfirmEmailViewModel
    {
        [Display(Name = "Fullname", Prompt = "Enter your fullname")]
        [Required(ErrorMessage = "Fullname is required")]
        public string FullName { get; set; }
        public string Id { get; set; }
        public string Token { get; set; }
    }
}
