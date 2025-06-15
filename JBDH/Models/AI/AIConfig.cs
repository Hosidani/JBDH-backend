namespace JBDH.Models;

public class AIConfig
{
    public string OpenAIKey { get; set; } = "sk-3c7fc5ab902a4832b4799b9f1d10fb0e";
    public string OpenAIModel { get; set; } = "deepseek-chat";
    public string OpenAIUrl { get; set; } = "https://api.deepseek.com/chat/completions";
    public string OpenAIOrgId { get; set; } = string.Empty;
    public string OpenAIUserId { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 1000;
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 1.0;
}