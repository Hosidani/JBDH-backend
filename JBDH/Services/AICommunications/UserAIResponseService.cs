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

            public async Task<List<UserAIResponse>> GetAllUserAIResponsesAsync(int userId)
            {
                return await _context.UserAIResponses
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
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