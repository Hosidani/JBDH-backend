using System.Text.Json;
using JBDH.Models;
using AIConfig = JBDH.Models.AIConfig;

    public class AIService
    {
        private readonly HttpClient _httpClient;
        private readonly AIConfig _aiConfig;

        public CreateAIconfig(IConfiguration configuration)
        {
            _aiConfig = new AIConfig
            {
                OpenAIKey = AISettings.GetAPIKey(),
                OpenAIModel = AISettings.GetModel(),
                OpenAIUrl = AISettings.GetAPIUrl(),
                OpenAIOrgId = AISettings.GetOrgId(),
                OpenAIUserId = AISettings.GetUserId(),
                MaxTokens = int.Parse(AISettings.GetMaxTokens()),
                Temperature = double.Parse(AISettings.GetTemperature()),
                TopP = AISettings.GetTopP()
            };
        }
        public AIService(HttpClient httpClient, AIConfig aiConfig)
        {
            _httpClient = httpClient;
            _aiConfig = aiConfig;
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            var requestBody = new
            {
                model = _aiConfig.OpenAIModel,
                prompt = prompt,
                max_tokens = _aiConfig.MaxTokens,
                temperature = _aiConfig.Temperature,
                top_p = _aiConfig.TopP
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            requestContent.Headers.Add("Authorization", $"Bearer {_aiConfig.OpenAIKey}");

            var response = await _httpClient.PostAsync(_aiConfig.OpenAIUrl, requestContent);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

            return jsonResponse.choices[0].text.ToString();
        }
    }