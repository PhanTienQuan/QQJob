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
        public List<Skill> Skills { get; set; } = new(); // Thêm dòng này để fix lỗi CS1061
    }
}
