using System.ComponentModel.DataAnnotations;

namespace QQJob.ViewModels.EmployerViewModels
{
    public class PostJobViewModel
    {
        [Required(ErrorMessage = "Post title is required")]
        [MinLength(10,ErrorMessage = "Post title needs to be longer than 10 characters.")]
        public string? Title { get; set; }
        [Required(ErrorMessage = "Description is required")]
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public string? Responsibilities { get; set; }
        [Required(ErrorMessage = "Location is required")]
        public string? Location { get; set; }
        public string? CustomLocation { get; set; }
        [Required(ErrorMessage = "Experience is required")]
        public float? Experience { get; set; }
        public float? CusExperience { get; set; }
        public string? Qualification { get; set; }
        [Required(ErrorMessage = "At least 1 skill needed")]
        public string SelectedSkill { get; set; }
        public string? Salary { get; set; }
        public string? PayType { get; set; }
        public string? CusPayType { get; set; }
        public string? WorkingHours { get; set; }
        public string? Benefits { get; set; }
        public string? WorkingType { get; set; }
        public string? CusWorkingType { get; set; }
        public DateTime? Close { get; set; }
        public int Opening { get; set; }
    }
}
