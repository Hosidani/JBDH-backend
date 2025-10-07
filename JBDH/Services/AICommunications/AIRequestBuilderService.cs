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
                    var cvText0 = await _manageIOService.ExtractTextFromPdf(aiRequest.CVPath ?? "");
                    userMessage = AIMessageTemplates.UserMessageTemplates[0].Replace("{CVText}", cvText0);
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage);

                case 1:
                    LoggingConfigService.ReturnInfo("CommandId is 1, processing job ad.");
                    var jobAdText1 = await _manageIOService.ExtractTextFromWebPage(aiRequest.JobAdUrl ?? "");
                    userMessage = AIMessageTemplates.UserMessageTemplates[1].Replace("{JobAdText}", jobAdText1);
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage);

                case 2:
                    LoggingConfigService.ReturnInfo("CommandId is 2, processing job ad and CV.");
                    var jobAdText2 = await _manageIOService.ExtractTextFromWebPage(aiRequest.JobAdUrl ?? "");
                    var cvText2 = await _manageIOService.ExtractTextFromPdf(aiRequest.CVPath ?? "");
                    userMessage = AIMessageTemplates.UserMessageTemplates[2]
                        .Replace("{JobAdText}", jobAdText2)
                        .Replace("{CVText}", cvText2);
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage);

                case 3:
                    LoggingConfigService.ReturnInfo("CommandId is 3, comparing UserAIResponse and JobAIResponse.");
                    var userAIResponse3 = aiRequest.UserAIResponse ?? "";
                    var jobAIResponse3 = aiRequest.JobAIResponse ?? "";
                    LoggingConfigService.ReturnInfo($"AIRequestBuilder: CommandId 3 is running. userAIResponse length: {userAIResponse3.Length}, jobAIResponse length: {jobAIResponse3.Length}");
                    userMessage = AIMessageTemplates.UserMessageTemplates[3]
                        .Replace("{UserAIResponse}", userAIResponse3)
                        .Replace("{JobAIResponse}", jobAIResponse3);
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage);

                case 4:
                    LoggingConfigService.ReturnInfo("CommandId is 4, proposing courses for user.");
                    var cvText4 = await _manageIOService.ExtractTextFromPdf(aiRequest.CVPath ?? "");
                    userMessage = AIMessageTemplates.UserMessageTemplates[4]
                        .Replace("{CVText}", cvText4);
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage);

                case 5:
                    LoggingConfigService.ReturnInfo("CommandId is 5, processing job ad and CV.");
                    LoggingConfigService.ReturnInfo(aiRequest.CVPath ?? "No CV path provided.");
                    var cvText3 = await _manageIOService.ExtractTextFromPdf(aiRequest.CVPath ?? "");
                    userMessage = AIMessageTemplates.UserMessageTemplates[5]
                        .Replace("{JobAdText}", aiRequest.JobAdText ?? "")
                        .Replace("{CVText}", cvText3 ?? "");
                    return Helpers.AIRequestHelper.BuildRequestBody(_aiConfig.OpenAIModel, userMessage);

                default:
                    return null;
            }
        }
    }
}