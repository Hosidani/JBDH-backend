using System.Text; 
using System.Text.Json;
using Newtonsoft.Json;
using JBDH.Models;
using JBDH.Services.AICommunications;
using AIConfig = JBDH.Models.AIConfig;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Nodes;
using JBDH.Services.Helpers;
using JBDH.Services.Configuration;

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
        public async Task<string> GetAIResponse(AIRequest request)
        {
            if (request == null)
            {
                LoggingConfigService.ReturnError("Request is null");
                return "Request is null";
            }
            if (request.User == null)
            {
                LoggingConfigService.ReturnError("User is null");
                return "User is null";
            }
            if (request.CommandId < 0)
            {
                LoggingConfigService.ReturnError("CommandId is invalid");
                return "CommandId is invalid";
            }
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _aiConfig.OpenAIKey);

            LoggingConfigService.ReturnInfo($"Getting AI response for user {request.User}.");
            if (_aiRequestBuilderService == null)
            {
                LoggingConfigService.ReturnError("AIHelperSwitchService is not initialized");
                return "AIHelperSwitchService is not initialized";
            }
            var content = await _aiRequestBuilderService!.AIRequestBuilder(request);
            if (content == null)
            {
                LoggingConfigService.ReturnError("Content is null or empty");
                return "Content is null or empty";
            }
            var response = await _httpClient.PostAsync(_aiConfig.OpenAIUrl, content);
            LoggingConfigService.ReturnInfo($"Response status code: {response.StatusCode}");
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(responseBody))
            {
                LoggingConfigService.ReturnError("Response body is null or empty");
                return "No response";
            }
            dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody!);
            return jsonResponse?.choices?[0]?.message?.content?.ToString() ?? "No response";
        }

    }
}