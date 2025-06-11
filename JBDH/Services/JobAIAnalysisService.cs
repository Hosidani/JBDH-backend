using JBDH.Models;
using JBDH.Services;
using System.Text;
public class JobAIAnalysisService : IJobAIAnalysisService
{
    private readonly IUserPropertiesService _userPropertiesService;
    private readonly IAIHelperSwitchService _aiHelperSwitchService;
    private static readonly AIConfig AIConfig = new AIConfig();

    public JobAIAnalysisService(
        IUserPropertiesService userPropertiesService,
        IAIHelperSwitchService aiHelperSwitchService)
    {
        _userPropertiesService = userPropertiesService;
        _aiHelperSwitchService = aiHelperSwitchService;
    }

    public async Task<StringContent> AnalyzeJobAsync(string jobAdUrl)
    {
        LoggingConfigService.ReturnInfo($"Analyzing job ad at URL: {jobAdUrl}");
        var user = await _userPropertiesService.GetCurrentUserAsync();
        var request = new AIHelperRequest
        {
            CommandId = 1,
            JobAdUrl = jobAdUrl,
            User = user
        };

        // Build the request body
        var requestContent = await _aiHelperSwitchService.HelperSwitch(request);

        // Send the request to the AI API endpoint
        using (var httpClient = new HttpClient())
        {
            var apiUrl = AIConfig.OpenAIUrl;
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AIConfig.OpenAIKey}");

            var response = await httpClient.PostAsync(apiUrl, requestContent);
            var responseContent = await response.Content.ReadAsStringAsync();
            return new StringContent(responseContent, Encoding.UTF8, "application/json");
        }
    }
}