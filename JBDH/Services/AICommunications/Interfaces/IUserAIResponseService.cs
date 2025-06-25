using System.Threading.Tasks;
using JBDH.Models.AI;
using JBDH.Models.User;

namespace JBDH.Services.AICommunications
{
    public interface IUserAIResponseService
    {
        AIResponse? GetAIResponseFromUser(User user);
        Task SaveUserAIResponseAsync(UserAIResponse aiResponse, User user);
        Task<UserAIResponse?> GetLatestUserAIResponseAsync(int userId);
    }
}