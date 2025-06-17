using QQJob.Models.Enum;
using System.ComponentModel.DataAnnotations;

namespace QQJob.ViewModels.EmployerViewModels
{
    public class EditJobViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Post title is required")]
        [MinLength(10,ErrorMessage = "Post title needs to be longer than 10 characters.")]
        public string? Title { get; set; }
        public JobDescription JobDescription { get; set; }
        public string? Address { get; set; }
        public string? CustomAddress { get; set; }
        public float? Experience { get; set; }
        public float? CusExperience { get; set; }
        public string? Qualification { get; set; }
        [Required(ErrorMessage = "At least 1 skill needed")]
        public string SelectedSkill { get; set; }
        public string? Salary { get; set; }
        public string? Benefits { get; set; }
        public string? CusWorkingSche { get; set; }
        public DateTime? Close { get; set; }
        public int Opening { get; set; }
    }
}
