using QQJob.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace QQJob.ViewModels.CandidateViewModels
{
    public class ResumeViewModal
    {
        public string? ResumeUrl { get; set; }
        public IFormFile? ResumeFile { get; set; }
        public List<Education> Educations { get; set; } = new();
        public List<Skill> Skills { get; set; } = new();
        public List<CandidateExp> Experiences { get; set; } = new();
        public List<Award> Awards { get; set; } = new();
    }
}
