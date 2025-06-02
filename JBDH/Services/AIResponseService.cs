using JBDH.Models;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Services
{
    public class AIResponseService : IAIResponseService
    {
        private readonly ApplicationDbContext _context;

        public AIResponseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public AIResponse GetAIResponseFromUser(JBDH.Models.User user)
        {
            if (user == null)
            {
                LoggingConfigService.ReturnError("User is null");
                return null;
            }
            var length = user.AIResponses.Count;
            if (length == 0)
            {
                LoggingConfigService.ReturnError("User AIResponses is empty");
                return null;
            }
            return user.AIResponses.ElementAtOrDefault(length - 1) ?? new AIResponse();
        }

        public void SaveAIResponse(AIResponse aiResponse, JBDH.Models.User user)
        {
            if (aiResponse == null || user == null)
            {
                LoggingConfigService.ReturnError("AIResponse or User is null");
                return;
            }

            _context.AIResponses.Add(aiResponse);
            _context.SaveChanges();

            LoggingConfigService.ReturnInfo($"AIResponse saved for user {user.Username}");
        }

        public async Task<UserAIResponse> GetLatestAIResponseAsync(int userId)
        {
            LoggingConfigService.ReturnInfo($"Fetching latest AI response for user ID: {userId}");
            if (userId <= 0)
            {
                LoggingConfigService.ReturnError("Invalid user ID provided.");
                return null;
            }
            return await _context.UserAIResponses
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.createdAt)
                .FirstOrDefaultAsync();
        }

    }
}