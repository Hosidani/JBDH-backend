using System.Text; 
using JBDH.Services;
using JBDH.Models;
using JBDH.Models.User;
using System.Text.Json.Nodes;
using System.Text.Json;
using Newtonsoft.Json; 

namespace JBDH.Services
{


    public class AIRequestBuilderService : IAIRequestBuilderService
    {
        private AIConfig _aiConfig;
        private readonly IManageInputService _manageInputService;
        public AIRequestBuilderService(IManageInputService manageInputService, AIConfig aiConfig)
        {
            _aiConfig = aiConfig;
            _manageInputService = manageInputService;
        }

        private static readonly Dictionary<int, string> UserMessageTemplates = new()
        {
            [0] = "Analyse this CV and tell me which job is the best fit for this candidate. " +
                  "Return ONLY a valid JSON object with the following fields: " +
                  "\"BestJobTitlesForUser\" (a short list, maximum 10 job titles), " +
                  "\"ReasoningBehindJobTitles\" (brief), " +
                  "\"HardSkills\" (a short list, maximum 10 skills), " +
                  "\"SoftSkills\" (a short list, maximum 10 skills), " +
                  "\"Experiences\" (a short list, maximum 10 experiences which states titles and approximate experience time), and " +
                  "\"EducationAndCertifications\" (a short list, maximum 10 items, brief).",

            [1] = "Analyse this web page for the job ad, look specifically at <div class=\"job-description\"> and tell me which job is the best fit for this candidate. " +
                  "Return ONLY a valid JSON object with the following fields: " +
                  "\"BestJobTitlesForUser\" (a short list, maximum 10 job titles), " +
                  "\"ReasoningBehindJobTitles\" (brief), " +
                  "\"HardRequirements\" (a short list, maximum 10 must-have requirements or skills), " +
                  "\"GoodToHaves\" (a short list, maximum 10 nice-to-have skills or traits), " +
                  "\"Experiences\" (a short list, maximum 10 relevant experiences), and " +
                  "\"EducationAndCertifications\" (a short list, maximum 10 items, brief).",

            [2] = "Analyze this web page for the job ad, look specifically at the <div class=\"job-description\">, summarize the provided CV, and tell me in what ways the candidate is a good fit for the job and what they might need to improve. " +
                  "Return ONLY a valid JSON object with the following fields: " +
                  "\"BestJobTitlesForUser\" (a short list, maximum 10 job titles), " +
                  "\"ReasoningBehindJobTitles\" (brief), " +
                  "\"HardSkills\" (a short list, maximum 10 skills), " +
                  "\"SoftSkills\" (a short list, maximum 10 skills), " +
                  "\"Experiences\" (a short list, maximum 10 experiences), and " +
                  "\"EducationAndCertifications\" (a short list, maximum 10 items, brief). " +
                  "I want you to be explicit with relevant experience as this could be a hard requirement to even be eligible to apply. If not eligible, then this needs to be very much focused on. Job ad URL: "
        };

        public async Task<StringContent?> AIRequestBuilder(AIRequest aiRequest)
        {
            string userMessage = string.Empty;
            if (aiRequest.CommandId == 0)
            {
                LoggingConfigService.ReturnInfo("CommandId is 0, processing CV.");
                userMessage = UserMessageTemplates[0];
                userMessage += await _manageInputService.ExtractTextFromPdf(aiRequest.CVPath ?? "");
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
                userMessage = UserMessageTemplates[1];
                userMessage += await _manageInputService.ExtractTextFromWebPage(aiRequest.JobAdUrl ?? "");
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
            if (aiRequest.CommandId == 2)
            {
                LoggingConfigService.ReturnInfo("CommandId is 2, processing job ad and CV.");
                userMessage = UserMessageTemplates[2];
                userMessage += await _manageInputService.ExtractTextFromWebPage(aiRequest.JobAdUrl ?? "");
                userMessage += " CV: ";
                userMessage += await _manageInputService.ExtractTextFromPdf(aiRequest.CVPath ?? "");
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