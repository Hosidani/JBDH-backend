using JBDH.Models;
namespace JBDH.Services
{
    public interface IAIResponseService
    {
        AIResponse GetAIResponseFromUser(User user);
        void SaveAIResponse(AIResponse aiResponse, User user);
        UserAIResponse GetUserAIResponseByUserId(int userId);
    }
}