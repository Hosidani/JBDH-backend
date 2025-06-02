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

        public async Task<StringContent> HelperSwitch(AIHelperRequest aiHelperRequest)
        {
            string userMessage = string.Empty;
            if (aiHelperRequest.CommandId == 0)
            {
                LoggingConfigService.ReturnInfo("CommandId is 0, processing CV.");
                userMessage = "Analyse this CV and tell me which job is the best fit for this candidate.";
                userMessage += await _manageInputService.ExtractTextFromPdf(aiHelperRequest.CVPath);
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
            if (aiHelperRequest.CommandId == 1)
            {
                LoggingConfigService.ReturnInfo("CommandId is 1, processing job ad.");
                userMessage = "Analyse this web page for the job ad, look specifically at <div class=\"job-description\"> and tell me which job is the best fit for this candidate.";
                userMessage += await _manageInputService.ExtractTextFromWebPage(aiHelperRequest.JobAdUrl);
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