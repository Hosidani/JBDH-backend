using JBDH.Models.AI;

namespace JBDH.Services.AICommunications
{
    public interface IAIApiClient
    {
        Task<AIResult> GetAIResponse(AIRequest request);
    }
}