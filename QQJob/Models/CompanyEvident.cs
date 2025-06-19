using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QQJob.Models
{
    public class CompanyEvident
    {

        [Key, ForeignKey("Employer")]
        public required string EmployerId { get; set; }
        public required string Url { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
