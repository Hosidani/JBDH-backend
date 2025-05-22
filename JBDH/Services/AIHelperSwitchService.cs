using System.Text; 
using JBDH.Services;
using JBDH.Models;
using System.Text.Json.Nodes;
using System.Text.Json;
using Newtonsoft.Json; 
namespace JBDH.Services
{


    public class AIHelperSwitchService : IAIHelperSwitchService
    {
        private AIConfig _aiConfig;
        private readonly IManageInputService _manageInputService;
        public AIHelperSwitchService(IManageInputService manageInputService, AIConfig aiConfig)
        {
            _aiConfig = aiConfig;
            _manageInputService = manageInputService;
        }

        public async Task<StringContent> HelperSwitch(User user, int commandId)
        {
            string userMessage = string.Empty;
            if (commandId == 0)
            {
                userMessage = "Analyse this CV and tell me which job is the best fit for this candidate.";
                userMessage += await _manageInputService.ExtractTextFromPdf(user.CVPath);
                // Create the request body
                var requestBody = new
                {
                    model = _aiConfig.OpenAIModel, // e.g., "deepseek-chat"
                    messages = new[]
                    {
                    new { role = "system", content = "You are to answer my questions." },
                    new { role = "user", content = userMessage }
                    },
                    stream = false
                };
                var requestJson = JsonConvert.SerializeObject(requestBody);
                var request = new StringContent(requestJson, Encoding.UTF8, "application/json");
                return request;
            }
            else
            {
                return null;
            }
            
        }
    }
}