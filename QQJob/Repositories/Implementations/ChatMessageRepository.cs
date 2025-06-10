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
                .ToListAsync();
        }

        public async Task UpdateIsReadAsync(Guid? chatId,string userId)
        {
            var messages = await _context.ChatMessages.Where(m => m.ChatId == chatId && m.SenderId != userId && !m.IsRead).ToListAsync();
            foreach(var message in messages)
            {
                message.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetChatSessionMessageCount(Guid chatId)
        {
            return await _context.ChatMessages.Where(m => m.ChatId == chatId).CountAsync();
        }

        public async Task<int> GetUnreadMessagesCount(Guid chatId,string userId)
        {
            return await _context.ChatMessages.Where(m => m.ChatId == chatId && m.SenderId == userId && !m.IsRead).CountAsync();
        }
    }
}
