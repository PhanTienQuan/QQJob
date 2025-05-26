namespace QQJob.Models
{
    public class ChatSession
    {
        public Guid ChatId { get; set; }
        public int User1Id { get; set; }
        public int User2Id { get; set; }

        public ICollection<ChatMessage> Messages { get; set; }
    }
}
