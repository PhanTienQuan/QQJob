namespace QQJob.Models
{
    public class ChatSession
    {
        public Guid ChatId { get; set; }
        public string User1Id { get; set; }
        public string User2Id { get; set; }
        public AppUser User1 { get; set; }
        public AppUser User2 { get; set; }
        public DateTime CreateAt { get; set; }

        public ICollection<ChatMessage> Messages { get; set; }
    }
}
