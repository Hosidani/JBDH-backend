using System.Threading.Tasks;
using JBDH.Models.AI;
using JBDH.Models.User;
using JBDH.Models.DTOs;
using System.Collections.Generic;

namespace JBDH.Services.AICommunications
{
    public interface IUserAIResponseService
    {
        Task SaveUserAIResponseAsync(UserAIResponse aiResponse, User user);
        Task<UserAIResponse?> GetLatestUserAIResponseAsync(int userId);
        Task<List<UserAIResponse>> GetAllUserAIResponsesAsync(int userId);
        Task<List<UserAIResponseDto>> GetAllUserAIResponseDtosAsync(int userId);
    }
}