using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class ChatMessageRepository(QQJobContext context):GenericRepository<ChatMessage>(context), IChatMessageRepository
    {
        public async Task<IEnumerable<ChatMessage>> GetChatMessage(Guid chatId,int skip,int take)
        {
            return await _context.ChatMessages
                .Where(m => m.ChatId == chatId)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.SentAt)
                .Skip(skip)
                .Take(take)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
    }
}
