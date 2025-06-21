using QQJob.Models.Enum;

namespace QQJob.ViewModels.EmployerViewModels
{
    public class ApplicantListViewModel
    {
        public List<ApplicantViewModel>? Applicants { get; set; }
        public PagingModel Paging { get; set; }
        public string? SearchValue { get; set; }
        public ApplicationStatus? SearchStatus { get; set; }
        public DateTime? FromDate { get; set; }
    }
}
