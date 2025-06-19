using QQJob.Models.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QQJob.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        public required string Content { get; set; }
        [ForeignKey("Receiver")]
        public string? ReceiverId { get; set; }
        public AppUser? Receiver { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsReaded { get; set; }
        public NotificationType Type { get; set; }
        public UserType UserType { get; set; }
    }
}
