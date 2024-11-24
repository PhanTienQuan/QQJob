using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QQJob.Models
{
    public class Employer
    {
        [Key]
        [Column("EmployerId")] // Đổi tên UserId thành EmployerId
        public string EmployerId { get; set; } // Đây vẫn ánh xạ với UserId của AspNetUsers

        public string EmployerName { get; set; }
        public string? Website { get; set; }
        public string? CompanyFiled { get; set; }
        public string? Description { get; set; }
        public DateTime FoundedDate { get; set; }
        public string? CompanySize { get; set; }

        // Navigation Property
        public AppUser User { get; set; }
        public ICollection<Job>? Jobs { get; set; }
        public ICollection<Follow>? Follows { get; set; }

    }
}
