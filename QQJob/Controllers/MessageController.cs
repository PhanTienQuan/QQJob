using Microsoft.AspNetCore.Mvc;
using QQJob.Models;
using QQJob.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QQJob.Controllers
{
    public class MessageController(IChatMessageRepository chatMessageRepository,IChatSessionRepository chatSessionRepository):Controller
    {
        [HttpGet]
        public async Task<JsonResult> GetMessages(Guid chatId,int skip = 0,int take = 10,Guid? previousChatId = null,string? currentUserId = null)
        {
            if(previousChatId.HasValue && currentUserId != null)
            {
                await chatMessageRepository.UpdateIsReadAsync(previousChatId,currentUserId);
            }

            var messages = await chatMessageRepository.GetChatMessage(chatId,skip,take);
            var chatSession = await chatSessionRepository.GetByIdAsync(chatId);
            var totalMessages = await chatMessageRepository.GetChatSessionMessageCount(chatId);
            bool hasMore = (skip + take) < totalMessages;

            string? otherUserId = null;

            if(chatSession != null && currentUserId != null)
            {

                if(chatSession.User1Id != currentUserId)
                {
                    otherUserId = chatSession.User1Id;
                }
                else
                {
                    otherUserId = chatSession.User2Id;
                }
            }

            bool otherUserAvailable = (otherUserId != null);


            var m = messages.Select(m => new
            {
                m.MessageId,
                m.ChatId,
                SenderId = string.IsNullOrEmpty(m.SenderId) ? null : m.SenderId,
                Avatar = string.IsNullOrEmpty(m.SenderId) ? null : m.Sender.Avatar,
                m.MessageText,
                m.SentAt
            });
            return Json(new { messages = m,hasMore,otherUserAvailable });
        }

        [HttpGet]
        public async Task<JsonResult> GetSessions(string userId,string? name = null,bool? isRead = null)
        {
            Expression<Func<ChatSession,bool>> predicate = s =>
                   (s.User1Id == userId || s.User2Id == userId) &&
                   (
                       string.IsNullOrEmpty(name) ||
                       (s.User1Id == userId && s.User2.FullName.Contains(name)) ||
                       (s.User2Id == userId && s.User1.FullName.Contains(name))
                   ) &&
                   (
                       !isRead.HasValue || // if null, skip filter
                       (isRead.Value
                           ? s.Messages.All(m => m.IsRead || m.SenderId == userId) // read = all messages are read (from other user)
                           : s.Messages.Any(m => !m.IsRead && m.SenderId != userId)) // unread = at least one message unread from other
                   );

            var sessions = await chatSessionRepository.GetChatSession(predicate,10,10);

            var sessionData = sessions.Select(async s => new
            {
                s.ChatId,
                s.User1Id,
                s.User2Id,
                s.User1,
                s.User2,
                s.CreateAt,
                s.Messages,
                unreadCount = await chatMessageRepository.GetUnreadMessagesCount(s.ChatId,s.User1Id != userId ? s.User1Id : s.User2Id)
            });
            return Json(new { sessions = sessionData });
        }
    }
}
