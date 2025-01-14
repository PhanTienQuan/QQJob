using System.ComponentModel.DataAnnotations;

namespace QQJob.ViewModels.EmployerViewModels
{
    public class PostJobViewModel
    {
        [Required(ErrorMessage = "Post title is required")]
        [MinLength(10,ErrorMessage = "Post title needs to be longer than 10 characters.")]
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? CustomLocation { get; set; }
        public float Experience { get; set; }
        public float? CusExperience { get; set; }
        public string? Qualification { get; set; }
        public string? Requirements { get; set; }
        public string? Responsibilities { get; set; }
        public string? SelectedSkill { get; set; }
        public string? Salary { get; set; }
        public string? Benefits { get; set; }
        public string? WorkingSche { get; set; }
        public string? CusWorkingSche { get; set; }
        public DateOnly Close { get; set; }
        public int Opening { get; set; }
    }
}
