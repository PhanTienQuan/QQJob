namespace QQJob.Models
{
    public class ChatMessage
    {
        public long MessageId { get; set; }
        public Guid ChatId { get; set; }
        public int SenderId { get; set; }
        public string MessageText { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public bool IsRead { get; set; } = false;

        public ChatSession ChatSession { get; set; }
    }
}
