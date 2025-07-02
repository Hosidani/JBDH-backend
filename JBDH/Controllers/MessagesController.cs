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
            int userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
                userId = int.Parse(userIdClaim.Value);
            else
                return Unauthorized();

            var messages = await _messageService.GetInboxAsync(userId);
            return View(messages);
        }

        // GET: /Messages/Conversation/{userId}
        public async Task<IActionResult> Conversation(int userId)
        {
            int currentUserId = 0;
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (currentUserIdClaim != null)
                currentUserId = int.Parse(currentUserIdClaim.Value);
            else
                return Unauthorized();

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
            int senderId = 0;
            var senderIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (senderIdClaim != null)
                senderId = int.Parse(senderIdClaim.Value);
            else
                return Unauthorized();

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