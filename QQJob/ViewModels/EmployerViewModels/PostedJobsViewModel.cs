using QQJob.Models.Enum;

namespace QQJob.ViewModels.EmployerViewModels
{
    public class PostedJobsViewModel
    {
        public List<PostedJobViewModel>? Jobs { get; set; }
        public PagingModel Paging { get; set; }
        public string? SearchValue { get; set; }
        public Status? SearchStatus { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
