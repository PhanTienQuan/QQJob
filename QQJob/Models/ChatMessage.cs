namespace QQJob.Models
{
    public class ChatMessage
    {
        public Guid MessageId { get; set; }
        public Guid ChatId { get; set; }
        public string? SenderId { get; set; }
        public AppUser? Sender { get; set; }
        public string MessageText { get; set; }
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; } = false;
        public bool IsRead { get; set; } = false;

        public ChatSession ChatSession { get; set; }
    }
}
