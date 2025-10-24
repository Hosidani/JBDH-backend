using Microsoft.AspNetCore.Mvc;
using JBDH.Swagger;
using JBDH.Services.Messaging;
using JBDH.Helpers;

namespace JBDH.Controllers
{
    [IncludeInApi]
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
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;
            if (!authResult.Value.HasValue)
                return this.ErrorPage("Authenticated user ID is missing.");

            var messages = await _messageService.GetInboxAsync(authResult.Value.Value);
            return View(messages);
        }

        // GET: /Messages/Conversation/{userId}
        public async Task<IActionResult> Conversation(int userId)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;
            if (!authResult.Value.HasValue)
                return this.ErrorPage("Authenticated user ID is missing.");

            var messages = await _messageService.GetConversationAsync(authResult.Value.Value, userId);
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
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;
            if (!authResult.Value.HasValue)
                return this.ErrorPage("Authenticated user ID is missing.");

            Console.WriteLine($"Sending from {authResult.Value.Value} to {recipientId}");

            if (recipientId == 0)
                return this.SetErrorAndRedirect("Invalid recipient.", "Inbox");

            await _messageService.SendMessageAsync(authResult.Value.Value, recipientId, content);
            return this.SetSuccessAndRedirect("Message sent!", "Conversation", new { userId = recipientId });
        }
    }
}