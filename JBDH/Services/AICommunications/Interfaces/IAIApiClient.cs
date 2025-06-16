using JBDH.Models;

namespace JBDH.Services
{
    public interface IAIApiClient
    {
        Task<string> GetAIResponse(AIRequest request);
    }
}