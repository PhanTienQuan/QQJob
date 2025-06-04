using QQJob.Models;

namespace QQJob.Repositories.Interfaces
{
    public interface IChatMessageRepository:IGenericRepository<ChatMessage>
    {
        public Task<IEnumerable<ChatMessage>> GetChatMessage(Guid chatId,int skip,int take);
    }
}
