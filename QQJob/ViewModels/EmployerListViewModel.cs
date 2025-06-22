using QQJob.Models.Enum;
using System.ComponentModel.DataAnnotations;

namespace QQJob.ViewModels
{
    public class EmployerListViewModel
    {
        public IEnumerable<EmployerViewModel> Employers { get; set; } = [];
        public PagingModel Paging { get; set; } = new PagingModel();
        [Display(Name = "Search by Employer Name")]
        public string? SearchEmployerName { get; set; }
        [Display(Name = "Search by Field")]
        public string? SearchField { get; set; }
        [Display(Name = "Search by Founded Date")]
        public int? SearchFoundedDate { get; set; }
        public bool Searching { get; set; } = false;
    }
}
