using JBDH.Models;

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

        // Method to get UserAIResponse by user ID
        public UserAIResponse GetUserAIResponseByUserId(int userId)
        {
            if (userId <= 0)
            {
                LoggingConfigService.ReturnError("Invalid user ID");
                return null;
            }

            var userAIResponse = _context.UserAIResponses
                .FirstOrDefault(uar => uar.UserId == userId);

            if (userAIResponse == null)
            {
                LoggingConfigService.ReturnError($"No AI response found for user ID {userId}");
            }

            return userAIResponse;
        }
    }
}