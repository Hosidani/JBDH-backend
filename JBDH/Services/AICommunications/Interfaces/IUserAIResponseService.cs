using System.Threading.Tasks;
using JBDH.Models;
using JBDH.Models.User;

public interface IUserAIResponseService
{
    AIResponse? GetAIResponseFromUser(User user);
    Task SaveUserAIResponseAsync(UserAIResponse aiResponse, User user);
    Task<UserAIResponse?> GetLatestUserAIResponseAsync(int userId);
}
