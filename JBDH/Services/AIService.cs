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

        // HOSSEIN: Deleted readonly (_aiConfig needs to be reassigned later.)
        /*
        curl.exe
        Calls the curl program. On Windows, you often see it as curl.exe.

        -X POST
        Specifies the HTTP method as POST (used to send data to the server).

        "https://api.deepseek.com/chat/completions"
        The URL endpoint for the DeepSeek API you're hitting.

        -H "Content-Type: application/json"
        Sets a header telling the server the body of the request is JSON formatted.

        -H "Authorization: Bearer sk-3c7fc5ab902a4832b4799b9f1d10fb0e"
        Sets the Authorization header using a Bearer token for authentication.

        -d '{...}'
        The request body, specified as a JSON string:

        EXPLANATION OF THE JSON BODY IN THE CURL COMMAND: 
        model: Specifies which model to use (deepseek-chat).

        messages: The conversation context.

        system message sets the behavior/context ("You.").

        user message is the prompt ("Hello!").

        stream: Set to false, meaning the API should respond with the full response at once rather than streaming. */
        //DONE: Can we add an example of the real CURL command here
        /*
        curl.exe -X POST "https://api.deepseek.com/chat/completions" 
        -H "Content-Type: application/json" 
        -H "Authorization: Bearer sk-3c7fc5ab902a4832b4799b9f1d10fb0e" `
        -d '{"model": "deepseek-chat", "messages": [{"role": "system", "content": "You."}, {"role": "user", "content": "Hello!"}], "stream": false}'
        */
        // Constructor to initialize HttpClient and AIConfig
        public AIService(HttpClient httpClient, AIConfig aiConfig, IManageInputService manageInputService, IAIHelperSwitchService aiHelperSwitchService)
        {
            _httpClient = httpClient;
            _aiConfig = aiConfig;
            _manageInputService = manageInputService;
            _aiHelperSwitchService = aiHelperSwitchService;

        }
        // this should move to a separate method
        public async Task<string> GetAIResponse(User user, int commandId) {

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _aiConfig.OpenAIKey);

        var content = await _aiHelperSwitchService.HelperSwitch(user,commandId);
        var response = await _httpClient.PostAsync(_aiConfig.OpenAIUrl, content);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
        return jsonResponse?.choices?[0]?.message?.content?.ToString() ?? "No response";
        }

    }
}