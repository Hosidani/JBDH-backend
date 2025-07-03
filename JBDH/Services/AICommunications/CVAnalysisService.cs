using JBDH.Models.User;
using JBDH.Services.AICommunications;
using JBDH.Services.Configuration;
using JBDH.Services.Helpers;
using JBDH.Models.AI;
using Newtonsoft.Json;


namespace JBDH.Services.AICommunications
{
    public class CVAnalysisService : ICVAnalysisService
    {
        private readonly IAIApiClient _aiApiClient;
        private readonly IUserAIResponseService _userAIResponseService;
        private readonly IManageIOService _manageIOService;

        public CVAnalysisService(IAIApiClient aiApiClient, IUserAIResponseService userAIResponseService, IManageIOService manageIOService)
        {
            _aiApiClient = aiApiClient;
            _userAIResponseService = userAIResponseService;
            _manageIOService = manageIOService;
        }

        public async Task AnalyzeAndSaveCVAsync(User user, string cvPath)
        {
            AIRequest request = new AIRequest
            {
                User = user,
                CommandId = 0,
                CVPath = cvPath
            };
            var aiResponseText = await _aiApiClient.GetAIResponse(request);
            LoggingConfigService.ReturnInfo($"Raw AI response: {aiResponseText}");
            CVAnalysisResult? analysisResult = null;
            try
            {
                aiResponseText = _manageIOService.ExtractJsonFromCodeBlock(aiResponseText);
                if (!string.IsNullOrWhiteSpace(aiResponseText) && aiResponseText.TrimStart().StartsWith("{"))
                {
                    analysisResult = JsonConvert.DeserializeObject<CVAnalysisResult>(aiResponseText);
                }
                else
                {
                    LoggingConfigService.ReturnError("AI response is not valid JSON.");
                }

                if (analysisResult != null)
                {
                    LoggingConfigService.ReturnInfo($"AI CVAnalysisResult for user {user.Id}: " +
                        $"BestJobTitlesForUser: {string.Join(", ", analysisResult.BestJobTitlesForUser ?? new List<string>())}; " +
                        $"HardSkills: {string.Join(", ", analysisResult.HardSkills ?? new List<string>())}; " +
                        $"SoftSkills: {string.Join(", ", analysisResult.SoftSkills ?? new List<string>())}; " +
                        $"Experiences: {string.Join(", ", (analysisResult.Experiences ?? new List<ExperienceItem>()).Select(e => $"{e.Title} ({e.Duration})"))}; " +
                        $"EducationAndCertifications: {string.Join(", ", analysisResult.EducationAndCertifications ?? new List<string>())}");
                }
                else
                {
                    LoggingConfigService.ReturnError("Deserialized CVAnalysisResult is null.");
                }
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Failed to deserialize AI response: {ex.Message}");
                return;
            }

            UserAIResponse useraiResponse = new UserAIResponse
            {
                ResponseText = aiResponseText,
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id,
                User = user
            };
            await _userAIResponseService.SaveUserAIResponseAsync(useraiResponse, user);
        }
    }
}