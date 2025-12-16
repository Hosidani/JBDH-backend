using JBDH.Models.AI;
using JBDH.Services.Configuration;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace JBDH.Services.Helpers
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly AIConfig _aiConfig;
        private readonly ILogger<EmbeddingService> _logger;

        public EmbeddingService(HttpClient httpClient, AIConfig aiConfig, ILogger<EmbeddingService> logger)
        {
            _httpClient = httpClient;
            _aiConfig = aiConfig;
            _logger = logger;

            // Configure authentication based on provider
            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            // Gemini uses x-goog-api-key header
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _aiConfig.GeminiApiKey);
        }

        /// <summary>
        /// Generate embedding vector for a given text using configured embedding provider
        /// Supports OpenAI and Gemini with task types for RAG optimization
        /// </summary>
        public async Task<float[]> GetEmbeddingAsync(string text, string taskType = "RETRIEVAL_DOCUMENT")
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("GetEmbeddingAsync received empty text");
                return new float[0];
            }

            try
            {
                _logger.LogInformation($"Requesting Gemini embedding for text length: {text.Length}");
                return await GetGeminiEmbeddingAsync(text, taskType);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating embedding: {ex.Message}");
                throw;
            }
        }

        // OpenAI method removed - using only Gemini

        private async Task<float[]> GetGeminiEmbeddingAsync(string text, string taskType)
        {
            // Gemini API request format with task type for RAG optimization
            var request = new
            {
                model = "models/gemini-embedding-001",
                content = new { parts = new[] { new { text = text } } },
                taskType = taskType // RETRIEVAL_DOCUMENT or RETRIEVAL_QUERY
            };

            _logger.LogInformation($"Sending Gemini request with taskType: {taskType}");

            var response = await _httpClient.PostAsJsonAsync(_aiConfig.EmbeddingUrl, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Gemini API error: {response.StatusCode} - {errorContent}");
                throw new Exception($"Gemini API returned {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonConvert.DeserializeObject<GeminiEmbeddingResponse>(responseContent);

            if (geminiResponse?.Embedding?.Values == null)
            {
                throw new Exception("No embedding data in Gemini response");
            }

            return geminiResponse.Embedding.Values;
        }

        /// <summary>
        /// Generate embeddings for multiple texts using Gemini (processes sequentially for now)
        /// </summary>
        public async Task<List<float[]>> GetEmbeddingsBatchAsync(List<string> texts, string taskType = "RETRIEVAL_DOCUMENT")
        {
            if (texts == null || texts.Count == 0)
            {
                _logger.LogWarning("GetEmbeddingsBatchAsync received empty list");
                return new List<float[]>();
            }

            try
            {
                _logger.LogInformation($"Requesting Gemini batch embeddings for {texts.Count} texts");
                var embeddings = new List<float[]>();

                // Process each text individually since Gemini API doesn't support batch requests
                foreach (var text in texts)
                {
                    var embedding = await GetGeminiEmbeddingAsync(text, taskType);
                    embeddings.Add(embedding);
                }

                return embeddings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating batch embeddings: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Calculate cosine similarity between two embedding vectors
        /// Formula: cos(A,B) = (A·B) / (||A|| * ||B||)
        /// Result: 0 (completely different) to 1 (identical)
        /// </summary>
        public float CalculateSimilarity(float[] vec1, float[] vec2)
        {
            if (vec1 == null || vec2 == null || vec1.Length == 0 || vec2.Length == 0)
            {
                _logger.LogWarning("CalculateSimilarity received null or empty vectors");
                return 0f;
            }

            if (vec1.Length != vec2.Length)
            {
                _logger.LogWarning($"CalculateSimilarity: vector dimensions mismatch {vec1.Length} vs {vec2.Length}");
                return 0f;
            }

            // Dot product: A·B
            float dotProduct = 0f;
            for (int i = 0; i < vec1.Length; i++)
            {
                dotProduct += vec1[i] * vec2[i];
            }

            // Magnitude of vec1: ||A||
            float magnitudeVec1 = 0f;
            for (int i = 0; i < vec1.Length; i++)
            {
                magnitudeVec1 += vec1[i] * vec1[i];
            }
            magnitudeVec1 = MathF.Sqrt(magnitudeVec1);

            // Magnitude of vec2: ||B||
            float magnitudeVec2 = 0f;
            for (int i = 0; i < vec2.Length; i++)
            {
                magnitudeVec2 += vec2[i] * vec2[i];
            }
            magnitudeVec2 = MathF.Sqrt(magnitudeVec2);

            // Avoid division by zero
            if (magnitudeVec1 == 0 || magnitudeVec2 == 0)
            {
                return 0f;
            }

            // Cosine similarity
            float similarity = dotProduct / (magnitudeVec1 * magnitudeVec2);

            _logger.LogInformation($"Calculated similarity: {similarity:F4}");

            return similarity;
        }
    }

    /// <summary>
    /// Response model from OpenAI Embedding API
    /// </summary>
    public class OpenAIEmbeddingResponse
    {
        [JsonProperty("object")]
        public string? Object { get; set; }

        [JsonProperty("data")]
        public List<EmbeddingData>? Data { get; set; }

        [JsonProperty("model")]
        public string? Model { get; set; }

        [JsonProperty("usage")]
        public Usage? Usage { get; set; }
    }

    public class EmbeddingData
    {
        [JsonProperty("object")]
        public string? Object { get; set; }

        [JsonProperty("embedding")]
        public float[]? Embedding { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }
    }

    public class Usage
    {
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }

    /// <summary>
    /// Response model from Gemini Embedding API
    /// </summary>
    public class GeminiEmbeddingResponse
    {
        [JsonProperty("embedding")]
        public GeminiEmbedding? Embedding { get; set; }
    }

    public class GeminiEmbedding
    {
        [JsonProperty("values")]
        public float[]? Values { get; set; }
    }
}
