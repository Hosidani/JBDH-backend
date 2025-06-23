using System.Collections.Generic;
using System.Threading.Tasks;
using JBDH.Models.Messaging;

namespace JBDH.Services.Messaging
{
    public interface IMessageService
    {
        Task SendMessageAsync(int senderId, int recipientId, string content);
        Task<List<Message>> GetConversationAsync(int userId1, int userId2);
        Task<List<Message>> GetInboxAsync(int userId);
    }
}