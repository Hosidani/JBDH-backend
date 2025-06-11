using JBDH.Models;

namespace JBDH.Services
{
    public interface IAIService
    {
        Task<string> GetAIResponse(AIHelperRequest request);
    }
}