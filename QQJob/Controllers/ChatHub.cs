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
            await chatMessageRepository.AddAsync(message);
            await chatMessageRepository.SaveChangesAsync();

            // Send to everyone in the session group
            await Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage",message);
        }

        public async Task JoinChat(Guid chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId,chatId.ToString());
        }

        public async Task LeaveChat(Guid chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId,chatId.ToString());
        }
    }
}
