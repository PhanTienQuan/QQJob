using QQJob.Models;
using QQJob.Models.Enum;

namespace QQJob.ViewModels
{
    public class JobListViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public JobDescription JobDes { get; set; }
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        public int AppliedCount { get; set; }
        public Status Status { get; set; }
        public IEnumerable<Skill> Skills { get; set; }
    }
}
