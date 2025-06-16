using JBDH.Models;
using JBDH.Models.User;

namespace JBDH.Services
{
    public interface IAIResponseService
    {
        AIResponse? GetAIResponseFromUser(User user);
        void SaveAIResponse(AIResponse aiResponse, User user);
        Task<UserAIResponse?> GetLatestAIResponseAsync(int userId);
    }
}