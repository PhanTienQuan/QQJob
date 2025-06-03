using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class ChatSessionRepository(QQJobContext context):GenericRepository<ChatSession>(context), IChatSessionRepository
    {
        public async Task<IEnumerable<ChatSession>> GetChatSession(string userID)
        {
            return await _context.ChatSessions
            .Where(s => s.User1Id == userID || s.User2Id == userID)
            .Include(s => s.Messages)
            .Include(s => s.User1)
            .Include(s => s.User2)
            .ToListAsync();
        }
    }
}
