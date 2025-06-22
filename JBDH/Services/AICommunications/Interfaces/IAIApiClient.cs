using JBDH.Models.AI;

namespace JBDH.Services.AICommunications
{
    public interface IAIApiClient
    {
        Task<string> GetAIResponse(AIRequest request);
    }
}