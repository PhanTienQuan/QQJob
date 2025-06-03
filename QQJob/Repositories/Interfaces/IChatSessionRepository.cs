using QQJob.Models;

namespace QQJob.Repositories.Interfaces
{
    public interface IChatSessionRepository
    {
        public Task<IEnumerable<ChatSession>> GetChatSession(string userID);
    }
}
