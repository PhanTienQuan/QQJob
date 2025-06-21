using System.ComponentModel.DataAnnotations;

namespace QQJob.ViewModels.EmployerViewModels
{
    public class PostJobViewModel
    {
        [Required(ErrorMessage = "Post title is required")]
        [MinLength(10,ErrorMessage = "Post title needs to be longer than 10 characters.")]
        public string? JobTitle { get; set; }
        [Required(ErrorMessage = "Description is required")]
        public string? Description { get; set; }
        [Required(ErrorMessage = "City/Province is required")]
        public string? City { get; set; }
        [Required(ErrorMessage = "Experience Level is required")]
        public string? ExperienceLevel { get; set; }
        public string SelectedSkill { get; set; }
        [Required(ErrorMessage = "Salary is required")]
        public string? Salary { get; set; }
        public string? SalaryType { get; set; }
        [Required(ErrorMessage = "JobType is required")]
        public string? JobType { get; set; }
        public DateTime? Close { get; set; }
        public int Opening { get; set; }
        public string? EmployerId { get; set; }
        public string? LocationRequirement { get; set; }
    }
}