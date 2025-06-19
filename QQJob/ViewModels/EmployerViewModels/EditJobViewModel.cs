using System.ComponentModel.DataAnnotations;

namespace QQJob.ViewModels.EmployerViewModels
{
    public class EditJobViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Post title is required")]
        [MinLength(10,ErrorMessage = "Post title needs to be longer than 10 characters.")]
        public string? JobTitle { get; set; }
        public string? Description { get; set; }
        public string? City { get; set; }
        public string? ExperienceLevel { get; set; }
        [Required(ErrorMessage = "At least 1 skill needed")]
        public string SelectedSkill { get; set; }
        public string? Salary { get; set; }
        public string? SalaryType { get; set; }
        public string? JobType { get; set; }
        public DateTime? Close { get; set; }
        public int Opening { get; set; }
        public string? LocationRequirement { get; set; }
    }
}
