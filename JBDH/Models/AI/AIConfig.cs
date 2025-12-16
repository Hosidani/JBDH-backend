using System;

namespace JBDH.Models.AI
{
    public class AIConfig
    {
        // DeepSeek API Configuration (for chat/analysis)
        public string DeepSeekApiKey { get; set; } = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") ?? "sk-c768a9b4f9a642f4bcad5063b0cf4b31";
        public string DeepSeekModel { get; set; } = "deepseek-chat";
        public string DeepSeekUrl { get; set; } = "https://api.deepseek.com/v1/chat/completions";

        // Gemini API Configuration (for embeddings)
        public string GeminiApiKey { get; set; } = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "AIzaSyAaIt2fidGPsqQ8JgEZjHr36-1lMjxZpJM";
        public string EmbeddingModel { get; set; } = "gemini-embedding-001";
        public string EmbeddingUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent";

        // Legacy properties for backward compatibility  
        public string OpenAIKey => DeepSeekApiKey; // For existing chat services
        public string OpenAIModel => DeepSeekModel; // For existing chat services  
        public string OpenAIUrl => DeepSeekUrl; // For existing chat services

        public string OpenAIOrgId { get; set; } = string.Empty;
        public string OpenAIUserId { get; set; } = string.Empty;
        public int MaxTokens { get; set; } = 1000;
        public double Temperature { get; set; } = 0.7;
        public double TopP { get; set; } = 1.0;
    }
}