using QQJob.Models;
using System.Linq.Expressions;

namespace QQJob.Repositories.Interfaces
{
    public interface IChatSessionRepository
    {
        public Task<IEnumerable<ChatSession>> GetChatSession(string userID);
        public Task<IEnumerable<ChatSession>> GetChatSession(string userID,int messageLimit,int sessionLimit);
        public Task<IEnumerable<ChatSession>> GetChatSession(Expression<Func<ChatSession,bool>> predict,int messageLimit,int sessionLimit);
        public Task<bool> UpdateRangeNullUserAsync(string userId);
    }
}
