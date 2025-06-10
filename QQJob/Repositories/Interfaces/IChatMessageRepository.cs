using QQJob.Models;

namespace QQJob.Repositories.Interfaces
{
    public interface IChatMessageRepository:IGenericRepository<ChatMessage>
    {
        public Task<IEnumerable<ChatMessage>> GetChatMessage(Guid chatId,int skip,int take);
        public Task UpdateIsReadAsync(Guid? chatId,string userId);
        public Task<int> GetChatSessionMessageCount(Guid chatId);
        public Task<int> GetUnreadMessagesCount(Guid chatId,string userId);
    }
}
