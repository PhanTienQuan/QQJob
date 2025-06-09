using Microsoft.AspNetCore.SignalR;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Controllers
{
    public class ChatHub(IChatMessageRepository chatMessageRepository,IAppUserRepository appUserRepository):Hub
    {
        public async Task SendMessage(Guid chatId,string senderId,string messageText)
        {
            var message = new ChatMessage
            {
                MessageId = Guid.NewGuid(),
                ChatId = chatId,
                SenderId = senderId,
                Sender = await appUserRepository.GetByIdAsync(senderId),
                MessageText = messageText,
                SentAt = DateTime.Now
            };
            await chatMessageRepository.UpdateIsReadAsync(chatId,senderId);
            await chatMessageRepository.AddAsync(message);
            await chatMessageRepository.SaveChangesAsync();
            int unreadMessagesCount = await chatMessageRepository.GetUnreadMessagesCount(chatId,senderId);
            // Send to everyone in the session group
            await Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage",new { message,unreadMessagesCount });
        }

        public async Task JoinChat(Guid chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId,chatId.ToString());
        }

        public async Task LeaveChat(Guid chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId,chatId.ToString());
        }

        public async Task UserTyping(string chatId,string senderId)
        {
            await Clients.OthersInGroup(chatId).SendAsync("ShowTyping",senderId);
        }

        public async Task UserStoppedTyping(string chatId,string senderId)
        {
            await Clients.OthersInGroup(chatId).SendAsync("HideTyping",senderId);
        }
    }
}
