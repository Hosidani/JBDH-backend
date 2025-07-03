using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.Messaging;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Services.Messaging
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendMessageAsync(int senderId, int recipientId, string content)
        {
            var sender = await _context.Users.FindAsync(senderId);
            var recipient = await _context.Users.FindAsync(recipientId);

            if (sender == null || recipient == null)
                throw new InvalidOperationException("Sender or recipient not found.");

            var message = new Message
            {
                SenderId = senderId,
                RecipientId = recipientId,
                Sender = sender,
                Recipient = recipient,
                Content = content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Message>> GetConversationAsync(int userId1, int userId2)
        {
            return await _context.Messages
                .Where(m => (m.SenderId == userId1 && m.RecipientId == userId2) ||
                            (m.SenderId == userId2 && m.RecipientId == userId1))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<List<Message>> GetInboxAsync(int userId)
        {
            return await _context.Messages
                .Where(m => m.RecipientId == userId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }
    }
}