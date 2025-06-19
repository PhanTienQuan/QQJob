using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QQJob.Models
{
    public class Employer
    {
        [Key]
        [Column("EmployerId")]
        public string EmployerId { get; set; }
        public string? Website { get; set; }
        public string? Description { get; set; }
        public DateTime FoundedDate { get; set; }
        public string? CompanySize { get; set; }
        public string? CompanyField { get; set; }
        public CompanyEvident? CompanyEvident { get; set; }

        // Navigation Property
        public AppUser User { get; set; }
        public ICollection<Job>? Jobs { get; set; }
        public ICollection<Follow>? Follows { get; set; }

    }
}
