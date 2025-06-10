using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QQJob.Repositories.Implementations
{
    public class ChatSessionRepository(QQJobContext context):GenericRepository<ChatSession>(context), IChatSessionRepository
    {
        public async Task<IEnumerable<ChatSession>> GetChatSession(string userID)
        {
            var sessions = await _context.ChatSessions
                .Where(s => s.User1Id == userID || s.User2Id == userID)
                .Include(s => s.Messages)
                .Include(s => s.User1)
                .Include(s => s.User2)
                .OrderByDescending(s => s.Messages.Any() ? s.Messages.Max(m => m.SentAt) : s.CreateAt)
                .ToListAsync();

            sessions ??= new List<ChatSession>();

            foreach(var session in sessions)
            {
                if(session.Messages != null && session.Messages.Any())
                {
                    session.Messages = session.Messages
                        .OrderByDescending(m => m.SentAt)
                        .OrderBy(m => m.SentAt)
                        .ToList();
                }
            }

            return sessions;
        }

        public async Task<IEnumerable<ChatSession>> GetChatSession(string userID,int messageLimit,int sessionLimit)
        {
            var sessions = await _context.ChatSessions
                .Where(s => s.User1Id == userID || s.User2Id == userID)
                .Include(s => s.Messages)
                .Include(s => s.User1)
                .Include(s => s.User2)
                .OrderByDescending(s => s.Messages.Any() ? s.Messages.Max(m => m.SentAt) : s.CreateAt)
                .Take(sessionLimit)
                .ToListAsync();

            sessions ??= new List<ChatSession>();

            foreach(var session in sessions)
            {
                if(session.Messages != null && session.Messages.Any())
                {
                    session.Messages = session.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Take(messageLimit)
                        .OrderBy(m => m.SentAt)
                        .ToList();
                }
            }

            return sessions;
        }

        public async Task<IEnumerable<ChatSession>> GetChatSession(Expression<Func<ChatSession,bool>> predicate,int messageLimit,int sessionLimit)
        {
            var sessions = await _context.ChatSessions
                .Where(predicate)
                .Include(s => s.Messages)
                .Include(s => s.User1)
                .Include(s => s.User2)
                .OrderByDescending(s => s.Messages.Any() ? s.Messages.Max(m => m.SentAt) : s.CreateAt)
                .Take(sessionLimit)
                .ToListAsync();

            foreach(var session in sessions)
            {
                if(session.Messages != null && session.Messages.Any())
                {
                    session.Messages = session.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Take(messageLimit)
                        .OrderBy(m => m.SentAt)
                        .ToList();
                }
            }

            return sessions;
        }

        public async Task<bool> UpdateRangeNullUserAsync(string userId)
        {
            using(var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var sessions = await _context.ChatSessions
                    .Where(cs => cs.User1Id == userId || cs.User2Id == userId)
                    .ToListAsync();

                    foreach(var session in sessions)
                    {
                        if(session.User1Id == userId) session.User1Id = null;
                        if(session.User2Id == userId) session.User2Id = null;
                    }

                    _context.ChatSessions.UpdateRange(sessions);

                    await SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch(Exception)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            }
        }
    }
}
