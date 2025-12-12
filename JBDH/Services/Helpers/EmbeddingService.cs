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

            // Add authentication header
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _aiConfig.OpenAIKey);
        }

        /// <summary>
        /// Generate embedding vector for a given text using DeepSeek API
        /// API Endpoint: https://api.deepseek.com/embeddings
        /// </summary>
        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("GetEmbeddingAsync received empty text");
                return new float[0];
            }

            try
            {
                _logger.LogInformation($"Requesting embedding for text length: {text.Length}");

                // DeepSeek API request format
                var request = new
                {
                    model = _aiConfig.EmbeddingModel, // "deepseek-embedding"
                    input = text,
                    encoding_format = "float"
                };

                _logger.LogInformation($"Sending request to: {_aiConfig.EmbeddingUrl}");

                // Make API call
                var response = await _httpClient.PostAsJsonAsync(
                    _aiConfig.EmbeddingUrl,
                    request
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"DeepSeek API error: {response.StatusCode} - {errorContent}");
                    throw new Exception($"DeepSeek API returned {response.StatusCode}: {errorContent}");
                }

                // Parse response
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"DeepSeek API response: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}...");

                var embeddingResponse = JsonConvert.DeserializeObject<DeepSeekEmbeddingResponse>(responseContent);

                if (embeddingResponse?.Data == null || embeddingResponse.Data.Count == 0)
                {
                    _logger.LogError("No embedding data in response");
                    throw new Exception("No embedding data received from API");
                }

                var embedding = embeddingResponse.Data[0].Embedding;
                if (embedding == null)
                {
                    _logger.LogError("Embedding data is null");
                    throw new Exception("Embedding data is null from API");
                }
                _logger.LogInformation($"Successfully generated embedding with {embedding.Length} dimensions");

                return embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating embedding: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generate embeddings for multiple texts in a single batch request (more efficient)
        /// </summary>
        public async Task<List<float[]>> GetEmbeddingsBatchAsync(List<string> texts)
        {
            if (texts == null || texts.Count == 0)
            {
                _logger.LogWarning("GetEmbeddingsBatchAsync received empty list");
                return new List<float[]>();
            }

            try
            {
                _logger.LogInformation($"Requesting batch embeddings for {texts.Count} texts");

                // DeepSeek API supports array of inputs
                var request = new
                {
                    model = _aiConfig.EmbeddingModel,
                    input = texts,
                    encoding_format = "float"
                };

                var response = await _httpClient.PostAsJsonAsync(
                    _aiConfig.EmbeddingUrl,
                    request
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"DeepSeek API batch error: {response.StatusCode} - {errorContent}");
                    throw new Exception($"DeepSeek API returned {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var embeddingResponse = JsonConvert.DeserializeObject<DeepSeekEmbeddingResponse>(responseContent);

                if (embeddingResponse?.Data == null)
                {
                    throw new Exception("No embedding data in batch response");
                }

                // Sort by index to maintain order and ensure non-null embeddings
                var sortedEmbeddings = new List<float[]>();
                foreach (var item in embeddingResponse.Data.OrderBy(e => e.Index))
                {
                    if (item.Embedding != null)
                    {
                        sortedEmbeddings.Add(item.Embedding);
                    }
                }

                _logger.LogInformation($"Successfully generated {sortedEmbeddings.Count} embeddings");

                return sortedEmbeddings;
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
    /// Response model from DeepSeek Embedding API
    /// </summary>
    public class DeepSeekEmbeddingResponse
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
}
