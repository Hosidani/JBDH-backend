using JBDH.Models.AI;

namespace JBDH.Services.AICommunications
{
    public interface IAIRequestBuilderService
    {
        Task<StringContent?> AIRequestBuilder(AIRequest aiRequest);
    }
}