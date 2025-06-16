using System.Threading.Tasks;
using JBDH.Models;

namespace JBDH.Services.AICommunications.Interfaces
{
    public interface IUserAIResponseService
    {
        AIResponse? GetAIResponseFromUser(User user);
        void SaveUserAIResponse(UserAIResponse aiResponse, User user);
        Task<UserAIResponse?> GetLatestUserAIResponseAsync(int userId);
    }
}