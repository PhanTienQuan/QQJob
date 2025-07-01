using QQJob.Models;
using QQJob.Models.Enum;

namespace QQJob.ViewModels.CandidateViewModels
{
    public class FollowingEmployerListViewModel
    {
        public List<Follow> Follows { get; set; }
        public PagingModel Paging { get; set; }
    }
}
