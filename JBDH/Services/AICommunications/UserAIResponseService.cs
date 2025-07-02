using System.Linq;
using System.Threading.Tasks;
using JBDH.Models.User;
using JBDH.Models.AI;
using JBDH.Models.Core;
using JBDH.Services.Configuration;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Services.AICommunications
{
    public class UserAIResponseService : IUserAIResponseService
    {
        private readonly ApplicationDbContext _context;

        public UserAIResponseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public AIResponse? GetAIResponseFromUser(User user)
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
            var userAIResponse = user.AIResponses.ElementAtOrDefault(length - 1);
            if (userAIResponse == null)
            {
                return null;
            }
            var aiResponse = new AIResponse
            {
                Id = userAIResponse.Id.ToString(), // Map Id to string
                ResponseText = userAIResponse.ResponseText, // Map Content to ResponseText
                CreatedAt = userAIResponse.CreatedAt
            };
            return aiResponse;
        }

        public void SaveUserAIResponse(UserAIResponse aiResponse, User user)
        {
            if (aiResponse == null || user == null)
            {
                LoggingConfigService.ReturnError("AIResponse or User is null");
                return;
            }
            _context.UserAIResponses.Add(aiResponse);
            _context.SaveChanges();
            LoggingConfigService.ReturnInfo($"AIResponse saved for user ID {user.Id}");
        }

        public async Task<UserAIResponse?> GetLatestUserAIResponseAsync(int userId)
        {
            return await _context.UserAIResponses
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task SaveUserAIResponseAsync(UserAIResponse aiResponse, User user)
        {
            if (aiResponse == null || user == null)
            {
                LoggingConfigService.ReturnError("AIResponse or User is null");
                return;
            }
            _context.UserAIResponses.Add(aiResponse);
            await _context.SaveChangesAsync();
            LoggingConfigService.ReturnInfo($"AIResponse saved for user ID {user.Id}");
        }
    }
}