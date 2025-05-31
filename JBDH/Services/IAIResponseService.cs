using JBDH.Models;
namespace JBDH.Services
{
    public interface IAIResponseService
    {
        AIResponse GetAIResponse();
        void SaveAIResponse(AIResponse aiResponse);
    }
}