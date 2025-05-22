using JBDH.Models;

namespace JBDH.Services
{
    public interface IAIService
    {
        Task<string> GetAIResponse(User user, int commandId);
    }
}