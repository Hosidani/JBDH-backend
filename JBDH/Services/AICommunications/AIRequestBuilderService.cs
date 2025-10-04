using System.Text;
using JBDH.Models.AI;
using Newtonsoft.Json; 
using JBDH.Services.Helpers;
using JBDH.Services.Configuration;

namespace JBDH.Services.AICommunications
{

    public class AIRequestBuilderService : IAIRequestBuilderService
    {
        private AIConfig _aiConfig;
        private readonly IManageIOService _manageIOService;
        public AIRequestBuilderService(IManageIOService manageIOService, AIConfig aiConfig)
        {
            _aiConfig = aiConfig;
            _manageIOService = manageIOService;
        }

    // ...existing code...

        public async Task<StringContent?> AIRequestBuilder(AIRequest aiRequest)
        {
            string userMessage = string.Empty;
            if (aiRequest.CommandId == 0)
            {
                LoggingConfigService.ReturnInfo("CommandId is 0, processing CV.");
                var cvText = await _manageIOService.ExtractTextFromPdf(aiRequest.CVPath ?? "");
                userMessage = AIMessageTemplates.UserMessageTemplates[0].Replace("{CVText}", cvText);
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
            if (aiRequest.CommandId == 1)
            {
                LoggingConfigService.ReturnInfo("CommandId is 1, processing job ad.");
                var jobAdText = await _manageIOService.ExtractTextFromWebPage(aiRequest.JobAdUrl ?? "");
                userMessage = AIMessageTemplates.UserMessageTemplates[1].Replace("{JobAdText}", jobAdText);
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
            if (aiRequest.CommandId == 2)
            {
                LoggingConfigService.ReturnInfo("CommandId is 2, processing job ad and CV.");
                var jobAdText = await _manageIOService.ExtractTextFromWebPage(aiRequest.JobAdUrl ?? "");
                var cvText = await _manageIOService.ExtractTextFromPdf(aiRequest.CVPath ?? "");
                userMessage = AIMessageTemplates.UserMessageTemplates[2]
                    .Replace("{JobAdText}", jobAdText)
                    .Replace("{CVText}", cvText);
                var requestBody = new
                {
                    model = _aiConfig.OpenAIModel,
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
            if (aiRequest.CommandId == 3)
            {
                LoggingConfigService.ReturnInfo("CommandId is 3, comparing UserAIResponse and JobAIResponse.");
                // These should be set in the AIRequest object
                var userAIResponse = aiRequest.UserAIResponse ?? "";
                var jobAIResponse = aiRequest.JobAIResponse ?? "";
                LoggingConfigService.ReturnInfo("AIRequestBuilder: CommandId 3 is running. userAIResponse length: " + userAIResponse.Length + ", jobAIResponse length: " + jobAIResponse.Length);
                userMessage = AIMessageTemplates.UserMessageTemplates[3]
                    .Replace("{UserAIResponse}", userAIResponse)
                    .Replace("{JobAIResponse}", jobAIResponse);
                var requestBody = new
                {
                    model = _aiConfig.OpenAIModel,
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
            if (aiRequest.CommandId == 4)
            {
                LoggingConfigService.ReturnInfo("CommandId is 4, proposing courses for user.");
                // Extract CV text
                var cvText = await _manageIOService.ExtractTextFromPdf(aiRequest.CVPath ?? "");
                userMessage = AIMessageTemplates.UserMessageTemplates[4]
                    .Replace("{CVText}", cvText);
                var requestBody = new
                {
                    model = _aiConfig.OpenAIModel,
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