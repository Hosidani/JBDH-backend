using JBDH.Models;
namespace JBDH.Services
{
    public class AIResponseService: IAIResponseService
    {


        public AIResponse GetAIResponse()
        {
            return new AIResponse
            {
                id = 1,
                responseText = "This is a sample AI response."
            };
        }
        public void SaveAIResponse(AIResponse aiResponse)
        {
            // Implementation for saving AI response to a file
        }

    }
}