using System.Text; 
using System.Text.Json;
using Newtonsoft.Json; 
using JBDH.Models;
using AIConfig = JBDH.Models.AIConfig;


    public class AIService
    {
        private readonly HttpClient _httpClient;
        private AIConfig _aiConfig; // HOSSEIN: Deleted readonly (_aiConfig needs to be reassigned later.)
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
        public AIService(HttpClient httpClient, AIConfig aiConfig)
        {
            _httpClient = httpClient;
            _aiConfig = aiConfig;
        }
        // Method to generate a response from OpenAI API
        public async Task<string> GenerateChatResponseAsync()
{
    string userMessage = "Hello! What is 5+2?"; // This should be replaced with the actual user input
    // Create the request body
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
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            // Add headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _aiConfig.OpenAIKey);

            var response = await _httpClient.PostAsync(_aiConfig.OpenAIUrl, requestContent);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

            return jsonResponse?.choices?[0]?.message?.content?.ToString() ?? "No response";
}

}