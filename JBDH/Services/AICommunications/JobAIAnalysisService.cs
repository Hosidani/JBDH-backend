using JBDH.Models;
using JBDH.Models.AI;
using JBDH.Services.Configuration;
using System.Text;
using Microsoft.EntityFrameworkCore;
using JBDH.Services.UserManagement;
using JBDH.Services.Helpers;
using JBDH.Models.Core;

namespace JBDH.Services.AICommunications
{

    public class JobAIAnalysisService : IJobAIAnalysisService
    {
        private readonly IUserPropertiesService _userPropertiesService;
        private readonly IAIRequestBuilderService _aiRequestBuilderService;
        private readonly IManageIOService _manageIOService;
        private readonly IAIApiClient _aiApiClient;
        private static readonly AIConfig AIConfig = new AIConfig();
        private readonly ApplicationDbContext _context;

        public JobAIAnalysisService(
            IUserPropertiesService userPropertiesService,
            IAIRequestBuilderService aIRequestBuilderService,
            ApplicationDbContext context,
            IManageIOService manageIOService,
            IAIApiClient aiApiClient)
        {
            _userPropertiesService = userPropertiesService;
            _aiRequestBuilderService = aIRequestBuilderService;
            _context = context;
            _manageIOService = manageIOService;
            _aiApiClient = aiApiClient;
        }

        public async Task<string> GetOrCreateAnalysisAsync(string jobAdUrl, int jobId)
        {
            // 1. Check for cached response in the database
            var cachedResponse = await _context.JobAIResponses
                .Where(r => r.JobId == jobId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (cachedResponse != null)
                return cachedResponse.ResponseText;

            // 2. Build and send AI request
            var user = await _userPropertiesService.GetCurrentUserAsync();
            var request = new AIRequest
            {
                CommandId = 1,
                JobAdUrl = jobAdUrl,
                User = user
            };
            var requestContent = await _aiRequestBuilderService.AIRequestBuilder(request);

            using (var httpClient = new HttpClient())
            {
                var apiUrl = AIConfig.OpenAIUrl;
                if (string.IsNullOrEmpty(AIConfig.OpenAIKey))
                    throw new InvalidOperationException("OpenAI API key is not configured.");

                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AIConfig.OpenAIKey}");

                var response = await httpClient.PostAsync(apiUrl, requestContent);

                if (response == null)
                    throw new Exception("HTTP response is null.");

                var responseContent = await response.Content.ReadAsStringAsync();
                if (responseContent == null)
                    throw new Exception("AI response content is null.");

                if (_manageIOService == null)
                    throw new Exception("_manageIOService is null!");

                var cleanJson = _manageIOService.ExtractInnerJsonFromJobAIResponse(responseContent);
                if (cleanJson == null)
                    throw new Exception("Extracted JSON is null.");

                // 3. Save the new response to the database
                var aiResponse = new JobAIResponse
                {
                    JobId = jobId,
                    ResponseText = cleanJson,
                    CreatedAt = DateTime.UtcNow
                };
                _context.JobAIResponses.Add(aiResponse);
                await _context.SaveChangesAsync();

                return cleanJson; 
            }
        }

        public async Task<string> GetCvJobComparisonAsync(string jobAdText, string cvPath)
        {
            var request = new AIRequest
            {
                CommandId = 5,
                CVPath = cvPath,
                JobAdText = jobAdText
            };
            var aiResult = await _aiApiClient.GetAIResponse(request); 
            return aiResult?.ToString() ?? string.Empty;
        }
    }
}