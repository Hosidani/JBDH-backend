using Newtonsoft.Json;
using AIConfig = JBDH.Models.AI.AIConfig;
using JBDH.Services.Helpers;
using JBDH.Services.Configuration;
using JBDH.Models.AI;

namespace JBDH.Services.AICommunications
{


    public class AIApiClient : IAIApiClient
    {
        private readonly HttpClient _httpClient;
        private AIConfig _aiConfig;
        private readonly IManageIOService _manageIOService;
        private readonly IAIRequestBuilderService _aiRequestBuilderService;

        public AIApiClient(HttpClient httpClient, AIConfig aiConfig, IManageIOService manageIOService, IAIRequestBuilderService aIRequestBuilderService)
        {
            _httpClient = httpClient;
            _aiConfig = aiConfig;
            _manageIOService = manageIOService;
            _aiRequestBuilderService = aIRequestBuilderService;
        }
        public async Task<AIResult> GetAIResponse(AIRequest request)
        {
            if (request == null)
            {
                LoggingConfigService.ReturnError("Request is null");
                return AIResult.ErrorResult("Request is null");
            }
            if (request.User == null)
            {
                if (request.CommandId == 4 || request.CommandId == 5)
                {
                    LoggingConfigService.ReturnInfo("User is null, but CommandId is 4 or 5, proceeding without user context."); 
                }
                else
                {
                    LoggingConfigService.ReturnError("User is null");
                    return AIResult.ErrorResult("User is null");
                }
                
            }
            if (request.CommandId < 0)
            {
                LoggingConfigService.ReturnError("CommandId is invalid");
                return AIResult.ErrorResult("CommandId is invalid");
            }
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _aiConfig.OpenAIKey);

            LoggingConfigService.ReturnInfo($"Getting AI response for user {request.User}.");
            if (_aiRequestBuilderService == null)
            {
                LoggingConfigService.ReturnError("AIHelperSwitchService is not initialized");
                return AIResult.ErrorResult("AIHelperSwitchService is not initialized");
            }
            var content = await _aiRequestBuilderService.AIRequestBuilder(request);
            if (content == null)
            {
                LoggingConfigService.ReturnError("Content is null or empty");
                return AIResult.ErrorResult("Content is null or empty");
            }
            var response = await _httpClient.PostAsync(_aiConfig.OpenAIUrl, content);
            LoggingConfigService.ReturnInfo($"Response status code: {response.StatusCode}");
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"HTTP error: {ex.Message}");
                return AIResult.ErrorResult($"HTTP error: {ex.Message}");
            }
            var responseBody = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(responseBody))
            {
                LoggingConfigService.ReturnError("Response body is null or empty");
                return AIResult.ErrorResult("No response");
            }
            dynamic? jsonResponse = null;
            try
            {
                jsonResponse = JsonConvert.DeserializeObject(responseBody!);
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Deserialization error: {ex.Message}");
                return AIResult.ErrorResult($"Deserialization error: {ex.Message}");
            }
            if (jsonResponse == null)
            {
                LoggingConfigService.ReturnError("Deserialization returned null");
                return AIResult.ErrorResult("No response");
            }
            var resultContent = jsonResponse.choices?[0]?.message?.content?.ToString();
            if (string.IsNullOrEmpty(resultContent))
            {
                return AIResult.ErrorResult("No response");
            }
            return AIResult.SuccessResult(resultContent);
        }
    }
}