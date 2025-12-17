using JBDH.Models.User;
using JBDH.Services.AICommunications;
using JBDH.Services.Configuration;
using JBDH.Services.Helpers;
using JBDH.Models.AI;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;


namespace JBDH.Services.AICommunications
{
    public class CVAnalysisService : ICVAnalysisService
    {
        private readonly IAIApiClient _aiApiClient;
        private readonly IUserAIResponseService _userAIResponseService;
        private readonly IManageIOService _manageIOService;
        private readonly ILogger<CVAnalysisService> _logger;

        public CVAnalysisService(IAIApiClient aiApiClient, IUserAIResponseService userAIResponseService, IManageIOService manageIOService, ILogger<CVAnalysisService> logger)
        {
            _aiApiClient = aiApiClient;
            _userAIResponseService = userAIResponseService;
            _manageIOService = manageIOService;
            _logger = logger;
        }

        public async Task AnalyzeAndSaveCVAsync(User user, string cvPath)
        {
            AIRequest request = new AIRequest
            {
                User = user,
                CommandId = 0,
                CVPath = cvPath
            };
            var aiResult = await _aiApiClient.GetAIResponse(request);
            var aiResponseText = aiResult.Response;
            LoggingConfigService.ReturnInfo($"Raw AI response: {aiResponseText}");
            
            // Always clean the AI response before saving
            var cleanedResponse = string.IsNullOrWhiteSpace(aiResponseText) 
                ? string.Empty 
                : _manageIOService.ExtractJsonFromCodeBlock(aiResponseText);
            LoggingConfigService.ReturnInfo($"Cleaned AI response (before deserialization): {cleanedResponse}");
            CVAnalysisResult? analysisResult = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(cleanedResponse))
                {
                    if (cleanedResponse.TrimStart().StartsWith("{"))
                    {
                        analysisResult = JsonConvert.DeserializeObject<CVAnalysisResult>(cleanedResponse);
                    }
                    else
                    {
                        LoggingConfigService.ReturnError("AI response is not valid JSON.");
                    }
                }
                else
                {
                    LoggingConfigService.ReturnError("AI response is null or whitespace.");
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
                ResponseText = cleanedResponse,
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id,
                User = user
            };
            await _userAIResponseService.SaveUserAIResponseAsync(useraiResponse, user);
        }

        public async Task<RequirementExtractionResponse?> ExtractCVProfessionAndRequirementsAsync(string cvText)
        {
            if (string.IsNullOrWhiteSpace(cvText))
            {
                _logger.LogWarning("CV text is empty or null");
                return null;
            }

            try
            {
                // Extract CV profession using AI
                var cvProfessionRequest = new AIRequest
                {
                    CommandId = 7, // Use same extraction logic for CV
                    JobAdText = cvText, // Use CV text as input
                    User = new User
                    {
                        Id = 1,
                        Email = "system@cv-analysis",
                        PasswordHash = "system",
                        Location = "system"
                    }
                };

                var cvProfessionResult = await _aiApiClient.GetAIResponse(cvProfessionRequest);

                if (cvProfessionResult.Success && !string.IsNullOrWhiteSpace(cvProfessionResult.Response))
                {
                    _logger.LogInformation("CV extraction response: {Response}", cvProfessionResult.Response);
                    
                    // Extract JSON from code blocks if needed
                    var cleanResponse = cvProfessionResult.Response;
                    if (cleanResponse.Contains("```json"))
                    {
                        var startIndex = cleanResponse.IndexOf("```json") + 7;
                        var endIndex = cleanResponse.LastIndexOf("```");
                        if (endIndex > startIndex)
                            cleanResponse = cleanResponse.Substring(startIndex, endIndex - startIndex).Trim();
                    }
                    
                    var cvExtracted = JsonConvert.DeserializeObject<RequirementExtractionResponse>(cleanResponse);
                    if (cvExtracted != null)
                    {
                        // Log CV extraction results in table format
                        _logger.LogInformation("\n" +
                            "════════════════════════════════════════════════════════════════\n" +
                            "                          CV EXTRACTION RESULTS                   \n" +
                            "════════════════════════════════════════════════════════════════\n" +
                            "PROFESSION:\n" +
                            "┌─────────────┬────────────────────────────────────────────────┐\n" +
                            "│ Field       │ Value                                          │\n" +
                            "├─────────────┼────────────────────────────────────────────────┤\n" +
                            "│ Main        │ {Main,-46} │\n" +
                            "│ Sub         │ {Sub,-46} │\n" +
                            "│ Specialty   │ {Specialty,-46} │\n" +
                            "└─────────────┴────────────────────────────────────────────────┘\n" +
                            "\n" +
                            "REQUIREMENTS ({RequirementCount} found):\n" +
                            "┌─────────────────────────────────────┬──────────────┬────────────┐\n" +
                            "│ Name                                │ Category     │ Importance │\n" +
                            "├─────────────────────────────────────┼──────────────┼────────────┤",
                            cvExtracted.Profession?.Main ?? "(empty)",
                            cvExtracted.Profession?.Sub ?? "(empty)",
                            cvExtracted.Profession?.Specialty ?? "(empty)",
                            cvExtracted.Requirements?.Count ?? 0);

                        // Log each requirement in table format
                        if (cvExtracted.Requirements != null && cvExtracted.Requirements.Count > 0)
                        {
                            foreach (var req in cvExtracted.Requirements)
                            {
                                _logger.LogInformation("│ {Name,-35} │ {Category,-12} │ {Importance,10} │",
                                    (req.Name.Length > 35 ? req.Name.Substring(0, 32) + "..." : req.Name),
                                    (req.Category.Length > 12 ? req.Category.Substring(0, 9) + "..." : req.Category),
                                    req.Importance);
                            }
                            _logger.LogInformation("└─────────────────────────────────────┴──────────────┴────────────┘");
                        }
                        else
                        {
                            _logger.LogInformation("│ No requirements extracted                                          │\n" +
                                                 "└─────────────────────────────────────┴──────────────┴────────────┘");
                        }
                        
                        _logger.LogInformation("════════════════════════════════════════════════════════════════\n");
                    }
                    
                    return cvExtracted;
                }
                else
                {
                    _logger.LogWarning("CV extraction failed: Success={Success}, Response={Response}", 
                        cvProfessionResult.Success, cvProfessionResult.Response);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CV extraction threw exception");
                return null;
            }
        }
    }
}