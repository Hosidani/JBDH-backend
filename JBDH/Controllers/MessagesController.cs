using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JBDH.Services.Messaging;

namespace JBDH.Controllers
{
    public class MessagesController : Controller
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        // GET: /Messages/Inbox
        public async Task<IActionResult> Inbox()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var messages = await _messageService.GetInboxAsync(userId);
            return View(messages);
        }

        // GET: /Messages/Conversation/{userId}
        public async Task<IActionResult> Conversation(int userId)
        {
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var messages = await _messageService.GetConversationAsync(currentUserId, userId);
            ViewBag.OtherUserId = userId;
            return View(messages);
        }

        // GET: /Messages/Send/{userId}
        public IActionResult Send(int userId)
        {
            ViewBag.RecipientId = userId;
            return View();
        }

        // POST: /Messages/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int recipientId, string content)
        {
            int senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            Console.WriteLine($"Sending from {senderId} to {recipientId}"); // For debugging

            if (recipientId == 0)
            {
                TempData["ErrorMessage"] = "Invalid recipient.";
                return RedirectToAction("Inbox");
            }

            await _messageService.SendMessageAsync(senderId, recipientId, content);
            return RedirectToAction("Conversation", new { userId = recipientId });
        }
    }
}