using System.Text; 
using System.Text.Json;
using Newtonsoft.Json; 
using JBDH.Models;
using AIConfig = JBDH.Models.AIConfig;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Nodes;

namespace JBDH.Services
{


    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private AIConfig _aiConfig;
        private readonly IManageInputService _manageInputService;
        private IAIHelperSwitchService _aiHelperSwitchService;
        public AIService(HttpClient httpClient, AIConfig aiConfig, IManageInputService manageInputService, IAIHelperSwitchService aiHelperSwitchService)
        {
            _httpClient = httpClient;
            _aiConfig = aiConfig;
            _manageInputService = manageInputService;
            _aiHelperSwitchService = aiHelperSwitchService;

        }
        // this should move to a separate method
        public async Task<string> GetAIResponse(AIHelperRequest request)
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
            var content = await _aiHelperSwitchService.HelperSwitch(request);
            if (content == null)
            {
                LoggingConfigService.ReturnError("Content is null or empty");
                return "Content is null or empty";
            }
            var response = await _httpClient.PostAsync(_aiConfig.OpenAIUrl, content);
            LoggingConfigService.ReturnInfo($"Response status code: {response.StatusCode}");
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            LoggingConfigService.ReturnInfo($"Response body: {responseBody}");
            dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
            return jsonResponse?.choices?[0]?.message?.content?.ToString() ?? "No response";
        }

    }
}