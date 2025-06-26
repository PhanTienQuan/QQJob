using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QQJob.Models;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels;
using System.Linq.Expressions;
using System.Security.Claims;

namespace QQJob.Controllers
{
    [Authorize]
    public class MessageController(IChatMessageRepository chatMessageRepository,IChatSessionRepository chatSessionRepository,IAppUserRepository appUserRepository,UserManager<AppUser> userManager):Controller
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
        [HttpGet]
        public async Task<IActionResult> ChatWith(string chatUserId)
        {
            string referer = Request.Headers["Referer"].ToString();

            var chatWithUser = await appUserRepository.GetByIdAsync(chatUserId);
            var currentUser = await userManager.GetUserAsync(User);

            // Check user exists
            if(chatWithUser == null)
            {
                TempData["Message"] = JsonConvert.SerializeObject(new { message = "User not found!",type = "error" });
                return Redirect(referer);
            }

            // Check not chatting with self
            if(chatWithUser.Id == currentUser.Id)
            {
                TempData["Message"] = JsonConvert.SerializeObject(new { message = "Can't chat with yourself!",type = "error" });
                return Redirect(referer);
            }

            // Find existing session
            var oldChatSession = await chatSessionRepository.FindAsync(s =>
                (s.User1Id == chatUserId && s.User2Id == currentUser.Id) ||
                (s.User2Id == chatUserId && s.User1Id == currentUser.Id));

            ChatSession chatSession;
            if(oldChatSession.Any())
            {
                chatSession = oldChatSession.First();
            }
            else
            {
                chatSession = new ChatSession
                {
                    User1Id = currentUser.Id,
                    User2Id = chatUserId,
                    CreateAt = DateTime.Now
                };
                await chatSessionRepository.AddAsync(chatSession);
                await chatSessionRepository.SaveChangesAsync();
            }

            var sessions = await chatSessionRepository.GetChatSession(currentUser.Id,10,10);

            MessageViewModel messageViewModel = new()
            {
                Sessions = sessions,
                CurrentChatSession = chatSession,
                CurrentUser = currentUser
            };

            return View("Index",messageViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Index(Guid? chatSessionId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessions = await chatSessionRepository.GetChatSession(userId,10,10);

            ChatSession? currentSession = null;
            if(chatSessionId.HasValue)
            {
                currentSession = sessions?.FirstOrDefault(s => s.ChatId == chatSessionId);
            }
            currentSession ??= sessions?.FirstOrDefault() ?? new ChatSession();

            var model = new MessageViewModel
            {
                Sessions = sessions,
                CurrentChatSession = currentSession,
                CurrentUser = await appUserRepository.GetByIdAsync(userId)
            };
            return View(model);
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetChatSessionIds()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var chatSessions = await chatSessionRepository.FindAsync(cs => cs.User1Id == userId || cs.User2Id == userId);
            var chatIds = chatSessions.Select(s => s.ChatId).ToArray();
            return Json(chatIds);
        }
    }
}
