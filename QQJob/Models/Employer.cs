using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QQJob.Models
{
    public class Employer
    {
        [Key]
        [Column("EmployerId")] // Đổi tên UserId thành EmployerId
        public string EmployerId { get; set; } // Đây vẫn ánh xạ với UserId của AspNetUsers
        public string? Website { get; set; }
        public string? CompanyField { get; set; }
        public string? Description { get; set; }
        public DateTime FoundedDate { get; set; }
        public string? CompanySize { get; set; }

        // Navigation Property
        public AppUser User { get; set; }
        public IEnumerable<Job>? Jobs { get; set; }
        public IEnumerable<Follow>? Follows { get; set; }

    }
}
