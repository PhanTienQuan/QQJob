using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class ChatMessageRepository(QQJobContext context):GenericRepository<ChatMessage>(context), IChatMessageRepository
    {
    }
}
