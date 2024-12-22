using QQJob.Models.Enum;

namespace QQJob.ViewModels
{
    public class PostedJobViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public JobDes JobDes { get; set; }
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        public int AppliedCount { get; set; }
        public Status Status { get; set; }
    }
}
