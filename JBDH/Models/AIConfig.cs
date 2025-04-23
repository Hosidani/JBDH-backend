namespace JBDH.Models;

public class AIConfig
{
    public string OpenAIKey { get; set; } = string.Empty;
    public string OpenAIModel { get; set; } = string.Empty;
    public string OpenAIUrl { get; set; } = string.Empty;
    public string OpenAIOrgId { get; set; } = string.Empty;
    public string OpenAIUserId { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 1000;
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 1.0;
}