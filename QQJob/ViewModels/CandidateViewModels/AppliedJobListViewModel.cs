using QQJob.Models;
using QQJob.Models.Enum;
using System.Collections.Generic;

namespace QQJob.ViewModels.CandidateViewModels
{
    public class AppliedJobListViewModel
    {
        public List<JobViewModel> Jobs { get; set; } = new();
        public PagingModel Paging { get; set; } = new();
    }
}