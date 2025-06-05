using QQJob.Models;
using QQJob.Models.Enum;

namespace QQJob.ViewModels
{
    public class JobDetailViewModel
    {
        public int Id { get; set; }
        public string EmployerId { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public JobDescription JobDes { get; set; }
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        public int AppliedCount { get; set; }
        public Status Status { get; set; }
        public IEnumerable<Skill> Skills { get; set; }
        public string Salary { get; set; }
        public int Opening { get; set; }
        public float Experience { get; set; }
        public string Qualification { get; set; }
        public string Benefits { get; set; }
        public string ImgUrl { get; set; }
        public string? PayType { get; set; }
        public string? WorkingHours { get; set; }
        public string? WorkingType { get; set; }
        public string? Website { get; set; }
    }
}
