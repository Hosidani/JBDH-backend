using JBDH.Models;

namespace JBDH.Services.AICommunications
{
    public interface IAIApiClient
    {
        Task<string> GetAIResponse(AIRequest request);
    }
}