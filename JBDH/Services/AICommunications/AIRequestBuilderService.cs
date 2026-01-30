using JBDH.Models.AI;
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

        public async Task<StringContent?> AIRequestBuilder(AIRequest aiRequest)
        {
            string userMessage = string.Empty;
            switch (aiRequest.CommandId)
            {
                case 0:
                    LoggingConfigService.ReturnInfo("CommandId is 0, processing CV.");
                    var cvText0 = await _manageIOService.ExtractTextFromFile(aiRequest.CVPath ?? "");
                    if (string.IsNullOrWhiteSpace(cvText0))
                    {
                        var err = "{\"error\":\"No CV text found to analyse. Please upload a CV before requesting analysis.\"}";
                        return new StringContent(err, System.Text.Encoding.UTF8, "application/json");
                    }
                    userMessage = AIMessageTemplates.UserMessageTemplates[0].Replace("{CVText}", cvText0);
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage, aiRequest.Language ?? "English");

                case 1:
                    LoggingConfigService.ReturnInfo("CommandId is 1, processing job ad.");
                    var jobAdUrl = aiRequest.JobAdUrl ?? "";
                    
                    // ADD: Log start of web scraping
                    LoggingConfigService.ReturnInfo($"🕷️ Starting web scraping for URL: {jobAdUrl}");
                    
                    var jobAdText1 = await _manageIOService.ExtractTextFromWebPage(jobAdUrl);
                    
                    // ADD: Log web scraping result
                    if (string.IsNullOrWhiteSpace(jobAdText1))
                    {
                        LoggingConfigService.ReturnError($"❌ Web scraping failed - no text extracted from: {jobAdUrl}");
                        var errorResponse = "{\"error\":\"No job ad text found. Web scraping failed.\"}";
                        return new StringContent(errorResponse, System.Text.Encoding.UTF8, "application/json");
                    }
                    else
                    {
                        LoggingConfigService.ReturnInfo($"✅ Web scraping completed for {jobAdUrl}, extracted {jobAdText1.Length} characters");
                    }
                    
                    userMessage = AIMessageTemplates.UserMessageTemplates[1].Replace("{JobAdText}", jobAdText1);
                    
                    // ADD: Log AI request preparation
                    LoggingConfigService.ReturnInfo($"📝 AI request built, prompt length: {userMessage.Length} characters");
                    
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage, aiRequest.Language ?? "English");

                case 2:
                    LoggingConfigService.ReturnInfo("CommandId is 2, processing job ad and CV.");
                    var jobAdText2 = await _manageIOService.ExtractTextFromWebPage(aiRequest.JobAdUrl ?? "");
                    var cvText2 = await _manageIOService.ExtractTextFromFile(aiRequest.CVPath ?? "");
                    if (string.IsNullOrWhiteSpace(cvText2))
                    {
                        var err2 = "{\"error\":\"No CV text found to analyse. Please upload a CV before requesting analysis.\"}";
                        return new StringContent(err2, System.Text.Encoding.UTF8, "application/json");
                    }
                    userMessage = AIMessageTemplates.UserMessageTemplates[2]
                        .Replace("{JobAdText}", jobAdText2)
                        .Replace("{CVText}", cvText2);
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage, aiRequest.Language ?? "English");

                case 3:
                    LoggingConfigService.ReturnInfo("CommandId is 3, comparing UserAIResponse and JobAIResponse.");
                    var userAIResponse3 = aiRequest.UserAIResponse ?? "";
                    var jobAIResponse3 = aiRequest.JobAIResponse ?? "";
                    LoggingConfigService.ReturnInfo($"AIRequestBuilder: CommandId 3 is running. userAIResponse length: {userAIResponse3.Length}, jobAIResponse length: {jobAIResponse3.Length}");
                    userMessage = AIMessageTemplates.UserMessageTemplates[3]
                        .Replace("{UserAIResponse}", userAIResponse3)
                        .Replace("{JobAIResponse}", jobAIResponse3);
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage, aiRequest.Language ?? "English");

                case 4:
                    LoggingConfigService.ReturnInfo("CommandId is 4, proposing courses for user.");
                    var cvText4 = await _manageIOService.ExtractTextFromFile(aiRequest.CVPath ?? "");
                    if (string.IsNullOrWhiteSpace(cvText4))
                    {
                        var err4 = "{\"error\":\"No CV text found to analyse. Please upload a CV before requesting course proposals.\"}";
                        return new StringContent(err4, System.Text.Encoding.UTF8, "application/json");
                    }
                    userMessage = AIMessageTemplates.UserMessageTemplates[4]
                        .Replace("{CVText}", cvText4);
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage, aiRequest.Language ?? "English");

                case 5:
                    LoggingConfigService.ReturnInfo("CommandId is 5, processing job ad and CV.");
                    LoggingConfigService.ReturnInfo(aiRequest.CVPath ?? "No CV path provided.");
                    var cvText3 = await _manageIOService.ExtractTextFromFile(aiRequest.CVPath ?? "");
                    if (string.IsNullOrWhiteSpace(cvText3))
                    {
                        var err3 = "{\"error\":\"No CV text found to analyse. Please upload a CV before requesting analysis.\"}";
                        return new StringContent(err3, System.Text.Encoding.UTF8, "application/json");
                    }
                    userMessage = AIMessageTemplates.UserMessageTemplates[5]
                        .Replace("{JobAdText}", aiRequest.JobAdText ?? "")
                        .Replace("{CVText}", cvText3 ?? "");
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage, aiRequest.Language ?? "English");

                case 6:
                    LoggingConfigService.ReturnInfo("CommandId is 6, extracting job requirements and importance.");
                    var jobAdText6 = aiRequest.JobAdText ?? "";
                    if (string.IsNullOrWhiteSpace(jobAdText6))
                    {
                        var err6 = "{\"error\":\"No job ad text found to analyse. Please provide job ad text.\"}";
                        return new StringContent(err6, System.Text.Encoding.UTF8, "application/json");
                    }
                    userMessage = AIMessageTemplates.UserMessageTemplates[6]
                        .Replace("{JobAdText}", jobAdText6);
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage, aiRequest.Language ?? "English");

                case 7:
                    LoggingConfigService.ReturnInfo("CommandId is 7, extracting CV profession and skills.");
                    var cvText7 = aiRequest.JobAdText ?? ""; // CV text is passed as JobAdText in this case
                    if (string.IsNullOrWhiteSpace(cvText7))
                    {
                        var err7 = "{\"error\":\"No CV text found to analyse. Please provide CV text.\"}";
                        return new StringContent(err7, System.Text.Encoding.UTF8, "application/json");
                    }
                    userMessage = AIMessageTemplates.UserMessageTemplates[7]
                        .Replace("{JobAdText}", cvText7);
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage, aiRequest.Language ?? "English");

                case 8:
                    LoggingConfigService.ReturnInfo("CommandId is 8, extracting job requirements and profession.");
                    var jobAdText8 = aiRequest.JobAdText ?? "";
                    if (string.IsNullOrWhiteSpace(jobAdText8))
                    {
                        var err8 = "{\"error\":\"No job ad text found to analyse. Please provide job ad text.\"}";
                        return new StringContent(err8, System.Text.Encoding.UTF8, "application/json");
                    }
                    userMessage = AIMessageTemplates.UserMessageTemplates[8]
                        .Replace("{JobAdText}", jobAdText8);
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage, aiRequest.Language ?? "English");

                default:
                    return null;
            }
        }
    }
}