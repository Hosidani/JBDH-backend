using System.Text; 
using JBDH.Services;
using JBDH.Models;
using JBDH.Models.User;
using System.Text.Json.Nodes;
using System.Text.Json;
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

        private static readonly Dictionary<int, string> UserMessageTemplates = new()
        {
            [0] = "Analyse this CV and tell me which job is the best fit for this candidate. " +
                  "Return ONLY a valid JSON object with the following fields: " +
                  "\"BestJobTitlesForUser\" (a short list, maximum 10 job titles), " +
                  "\"ReasoningBehindJobTitles\" (brief), " +
                  "\"HardSkills\" (a short list, maximum 10 skills), " +
                  "\"SoftSkills\" (a short list, maximum 10 skills), " +
                  "\"Experiences\" (a short list, maximum 10 experiences which states titles and approximate experience time), and " +
                  "\"EducationAndCertifications\" (a short list, maximum 10 items, brief). " +
                  "\n\nCV:\n{CVText}",

            [1] = "Analyze the following job advertisement. " +
                  "Extract and summarize the key requirements and characteristics of the ideal candidate. " +
                  "Return ONLY a valid JSON object with the following fields: " +
                  "\"JobTitle\" (a short list, maximum 10 relevant job titles which are suitable to the job annons.), " +
                  "\"HardRequirements\" (a short list, maximum 10 must-have requirements or skills), " +
                  "\"GoodToHaves\" (a short list, maximum 10 nice-to-have skills or traits), " +
                  "\"Experiences\" (a short list, maximum 10 relevant experiences), " +
                  "\"EducationAndCertifications\" (a short list, maximum 10 items, brief), " +
                  "\"JobSummary\" (a brief summary of the job). " +
                  "\n\nJob Ad:\n{JobAdText}",

            [2] = "You are given a job advertisement and a candidate's CV. " +
                  "Compare the candidate's qualifications, skills, and experiences to the requirements in the job ad. " +
                  "Be explicit about where the candidate is a strong fit and where there are gaps or missing requirements. " +
                  "Return ONLY a valid JSON object with the following fields: " +
                  "\"FitSummary\" (brief summary of fit), " +
                  "\"MissingRequirements\" (list of gaps or missing requirements), " +
                  "\"MatchingSkills\" (list of skills from the CV that match the job ad), " +
                  "\"RelevantExperiences\" (list of relevant experiences from the CV), " +
                  "\"EducationAndCertifications\" (from the CV, relevant to the job ad). " +
                  "\n\nJob Ad:\n{JobAdText}\n\nCV:\n{CVText}",
        };      

        public async Task<StringContent?> AIRequestBuilder(AIRequest aiRequest)
        {
            string userMessage = string.Empty;
            if (aiRequest.CommandId == 0)
            {
                LoggingConfigService.ReturnInfo("CommandId is 0, processing CV.");
                var cvText = await _manageIOService.ExtractTextFromPdf(aiRequest.CVPath ?? "");
                userMessage = UserMessageTemplates[0].Replace("{CVText}", cvText);

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
                userMessage = UserMessageTemplates[1].Replace("{JobAdText}", jobAdText);

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

                userMessage = UserMessageTemplates[2]
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
            else
            {
                return null;
            }
        }
    }
}