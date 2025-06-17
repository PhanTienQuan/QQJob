using QQJob.Models;

namespace QQJob.ViewModels
{
    public class MessageViewModel
    {
        public IEnumerable<ChatSession>? Sessions { get; set; }
        public ChatSession? CurrentChatSession { get; set; }
        public AppUser CurrentUser { get; set; }
    }
}
