using System.ComponentModel.DataAnnotations;

namespace QQJob.Models
{
    public class FunctionalArea
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public IEnumerable<Employer>? Employers { get; set; }
    }
}
