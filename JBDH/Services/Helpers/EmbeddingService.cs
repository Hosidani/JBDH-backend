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
        
        // Simple in-memory cache to reduce API calls - TODO: Replace with Redis/distributed cache
        private readonly Dictionary<string, float[]> _embeddingCache = new Dictionary<string, float[]>();

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

            // Simple cache key - TODO: Make more sophisticated
            var cacheKey = $"{text}|{taskType}";
            
            if (_embeddingCache.TryGetValue(cacheKey, out var cachedEmbedding))
            {
                _logger.LogInformation($"Cache HIT for text length: {text.Length}");
                return cachedEmbedding;
            }

            try
            {
                _logger.LogInformation($"Cache MISS - Requesting Gemini embedding for text length: {text.Length}");
                var embedding = await GetGeminiEmbeddingAsync(text, taskType);
                
                // Cache the result
                _embeddingCache[cacheKey] = embedding;
                
                return embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating embedding: {ex.Message}");
                
                // Check if it's a quota error and provide helpful message
                if (ex.Message.Contains("TooManyRequests") || ex.Message.Contains("429"))
                {
                    _logger.LogError("QUOTA EXCEEDED: You've hit the Gemini API free tier limit. Using fallback logic.");
                    
                    // Return a dummy embedding for testing - TODO: Implement proper fallback
                    return GetFallbackEmbedding(text);
                }
                
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

        /// <summary>
        /// Compare CV skills with job requirements using embeddings
        /// </summary>
        public async Task<(float skillsMatchPercentage, List<object> detailedMatches)> CompareSkillsWithEmbeddingsAsync(
            List<JBDH.Models.AI.Requirement> cvRequirements, 
            List<JBDH.Models.AI.Requirement> jobRequirements)
        {
            if (jobRequirements.Count == 0)
            {
                _logger.LogWarning("No job requirements provided for skill comparison");
                return (0f, new List<object>());
            }

            // Generate embeddings for skill-to-skill matching
            var cvSkillTexts = cvRequirements.Select(r => r.Name).ToList();
            var cvSkillVectors = cvSkillTexts.Count > 0 
                ? await GetEmbeddingsBatchAsync(cvSkillTexts, "RETRIEVAL_DOCUMENT")
                : new List<float[]>();

            var requirementTexts = jobRequirements.Select(r => r.Name).ToList();
            var requirementVectors = await GetEmbeddingsBatchAsync(requirementTexts, "RETRIEVAL_QUERY");

            // Skill-to-skill matching + weight calculation
            var results = new List<object>();
            float weightedScoreSum = 0f;
            float importanceSum = 0f;

            for (int i = 0; i < jobRequirements.Count; i++)
            {
                var req = jobRequirements[i];
                float bestSimilarity = 0f;
                string? matchedCvSkill = null;
                int matchedCvSkillImportance = 0;

                // Find best matching CV skill for this job requirement
                for (int j = 0; j < cvSkillVectors.Count; j++)
                {
                    var similarity = CalculateSimilarity(
                        cvSkillVectors[j],
                        requirementVectors[i]
                    );

                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        matchedCvSkill = cvRequirements[j].Name;
                        matchedCvSkillImportance = cvRequirements[j].Importance;
                    }
                }

                // For now, use only embedding similarity without keyword fallback
                var finalSimilarity = bestSimilarity;
                var keywordScore = 0f; // Keep for logging purposes
                var weightedScore = finalSimilarity * req.Importance;

                weightedScoreSum += weightedScore;
                importanceSum += req.Importance;

                results.Add(new
                {
                    requirement = req.Name,
                    category = req.Category,
                    importance = req.Importance,
                    matchedCvSkill = matchedCvSkill ?? "No match",
                    cvSkillLevel = matchedCvSkillImportance,
                    vectorSimilarity = (float)Math.Round(bestSimilarity, 4),
                    keywordScore = (float)Math.Round(keywordScore, 4),
                    finalSimilarity = (float)Math.Round(finalSimilarity, 4),
                    weightedScore = (float)Math.Round(weightedScore, 4)
                });
            }

            var matchPercentage = importanceSum > 0
                ? (float)Math.Round((weightedScoreSum / importanceSum) * 100, 2)
                : 0f;

            _logger.LogInformation("Skills comparison complete - Match percentage: {MatchPercentage}%, Requirements: {Count}", 
                matchPercentage, jobRequirements.Count);

            return (matchPercentage, results);
        }

        /// <summary>
        /// Calculate profession compatibility score using embeddings
        /// </summary>
        public async Task<float> CalculateProfessionScoreWithEmbeddingsAsync(Models.AI.Profession? cvProfession, Models.AI.Profession? jobProfession)
        {
            if (cvProfession == null || jobProfession == null)
                return 0.05f; // Very low score if missing

            // Calculate each level separately with proper weights
            float mainScore = 0f;
            float subScore = 0f;
            float specialtyScore = 0f;

            // 1. MAIN profession comparison (50% weight)
            if (!string.IsNullOrWhiteSpace(cvProfession.Main) && !string.IsNullOrWhiteSpace(jobProfession.Main))
            {
                if (string.Equals(cvProfession.Main?.Trim(), jobProfession.Main?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    mainScore = 1.0f; // Perfect match
                }
                else
                {
                    // Use embedding similarity AS IS - no arbitrary scaling!
                    var cvMainVector = await GetEmbeddingAsync(cvProfession.Main, "RETRIEVAL_DOCUMENT");
                    var jobMainVector = await GetEmbeddingAsync(jobProfession.Main, "RETRIEVAL_QUERY");
                    mainScore = CalculateSimilarity(cvMainVector, jobMainVector);
                    
                    _logger.LogInformation("Main profession similarity: CV='{CvMain}' vs Job='{JobMain}' = {Similarity:F4} (AS IS)", 
                        cvProfession.Main, jobProfession.Main, mainScore);
                }
            }

            // 2. SUB profession comparison (30% weight)
            if (!string.IsNullOrWhiteSpace(cvProfession.Sub) && !string.IsNullOrWhiteSpace(jobProfession.Sub))
            {
                if (string.Equals(cvProfession.Sub?.Trim(), jobProfession.Sub?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    subScore = 1.0f; // Perfect match
                }
                else
                {
                    var cvSubVector = await GetEmbeddingAsync(cvProfession.Sub, "RETRIEVAL_DOCUMENT");
                    var jobSubVector = await GetEmbeddingAsync(jobProfession.Sub, "RETRIEVAL_QUERY");
                    subScore = CalculateSimilarity(cvSubVector, jobSubVector);
                    
                    _logger.LogInformation("Sub profession similarity: CV='{CvSub}' vs Job='{JobSub}' = {Similarity:F4} (AS IS)", 
                        cvProfession.Sub, jobProfession.Sub, subScore);
                }
            }
            else if (string.IsNullOrWhiteSpace(cvProfession.Sub) || string.IsNullOrWhiteSpace(jobProfession.Sub))
            {
                // If one is missing, use main score as fallback
                subScore = mainScore;
            }

            // 3. SPECIALTY profession comparison (20% weight)  
            if (!string.IsNullOrWhiteSpace(cvProfession.Specialty) && !string.IsNullOrWhiteSpace(jobProfession.Specialty))
            {
                if (string.Equals(cvProfession.Specialty?.Trim(), jobProfession.Specialty?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    specialtyScore = 1.0f; // Perfect match
                }
                else
                {
                    var cvSpecVector = await GetEmbeddingAsync(cvProfession.Specialty, "RETRIEVAL_DOCUMENT");
                    var jobSpecVector = await GetEmbeddingAsync(jobProfession.Specialty, "RETRIEVAL_QUERY");
                    specialtyScore = CalculateSimilarity(cvSpecVector, jobSpecVector);
                    
                    _logger.LogInformation("Specialty profession similarity: CV='{CvSpecialty}' vs Job='{JobSpecialty}' = {Similarity:F4} (AS IS)", 
                        cvProfession.Specialty, jobProfession.Specialty, specialtyScore);
                }
            }
            else
            {
                // If specialty missing, use sub score as fallback
                specialtyScore = subScore;
            }

            // 4. Weighted combination: Main(50%) + Sub(30%) + Specialty(20%)
            var totalScore = (mainScore * 0.5f) + (subScore * 0.3f) + (specialtyScore * 0.2f);
            
            _logger.LogInformation("Profession scoring breakdown: Main={Main:F2}(50%) + Sub={Sub:F2}(30%) + Specialty={Specialty:F2}(20%) = {Total:F2}", 
                mainScore, subScore, specialtyScore, totalScore);

            // Apply domain-aware profession matching
            var domainBonus = GetProfessionDomainBonus(cvProfession.Main, jobProfession.Main);
            var adjustedScore = Math.Min(totalScore + domainBonus, 1.0f);
            
            _logger.LogInformation("Domain bonus applied: {DomainBonus:F3}, Adjusted score: {AdjustedScore:F3}", 
                domainBonus, adjustedScore);

            // Apply penalties based on adjusted score
            if (adjustedScore < 0.2f)
            {
                _logger.LogInformation("Completely unrelated professions (adjusted < 20%), applying severe penalty");
                return 0.05f; // Nearly zero for completely different fields
            }
            else if (adjustedScore < 0.3f)
            {
                _logger.LogInformation("Very different professions (adjusted < 30%), applying major penalty");
                return Math.Max(adjustedScore * 0.2f, 0.05f); // 80% penalty
            }
            else if (adjustedScore < 0.4f)
            {
                _logger.LogInformation("Different professional fields detected (adjusted < 40%), applying moderate penalty");
                return Math.Max(adjustedScore * 0.6f, 0.05f); // 40% penalty
            }

            return adjustedScore;
        }

        /// <summary>
        /// Calculate domain-specific bonus for related professions
        /// NOTE: This is experimental test logic to handle profession matching edge cases
        /// TODO: Replace with more sophisticated domain classification system
        /// </summary>
        private float GetProfessionDomainBonus(string? cvMain, string? jobMain)
        {
            if (string.IsNullOrWhiteSpace(cvMain) || string.IsNullOrWhiteSpace(jobMain))
                return 0f;

            var cv = cvMain.ToLowerInvariant();
            var job = jobMain.ToLowerInvariant();

            // EXPERIMENTAL: Tech domain relationships - hardcoded for testing
            var techTerms = new[] { "developer", "engineer", "programmer", "coder", "architect" };
            var isCvTech = techTerms.Any(term => cv.Contains(term));
            var isJobTech = techTerms.Any(term => job.Contains(term));

            if (isCvTech && isJobTech)
            {
                // TEST: High bonus for fullstack <-> backend/frontend relationship
                // This solves the "fullstack should match backend" problem
                if ((cv.Contains("fullstack") || cv.Contains("full stack")) && 
                    (job.Contains("backend") || job.Contains("frontend") || job.Contains("back end") || job.Contains("front end")))
                    return 0.25f; // TEST: 25% bonus - may need tuning
                
                if ((job.Contains("fullstack") || job.Contains("full stack")) && 
                    (cv.Contains("backend") || cv.Contains("frontend") || cv.Contains("back end") || cv.Contains("front end")))
                    return 0.25f; // TEST: 25% bonus - may need tuning

                // TEST: Medium bonus for exact specialization matches
                if ((cv.Contains("backend") && job.Contains("backend")) || 
                    (cv.Contains("frontend") && job.Contains("frontend")) ||
                    (cv.Contains("back end") && job.Contains("back end")) ||
                    (cv.Contains("front end") && job.Contains("front end")))
                    return 0.15f; // TEST: 15% bonus - may need tuning

                // TEST: Small bonus for same general tech field
                return 0.1f; // TEST: 10% bonus for any tech-to-tech - baseline
            }

            // TEST: Medical domain relationships - basic implementation
            var medicalTerms = new[] { "doctor", "physician", "nurse", "medical", "healthcare", "clinical" };
            var isCvMedical = medicalTerms.Any(term => cv.Contains(term));
            var isJobMedical = medicalTerms.Any(term => job.Contains(term));

            if (isCvMedical && isJobMedical)
                return 0.15f; // TEST: 15% bonus for medical-to-medical

            // TEST: Cross-domain penalty - this solves the "developer vs doctor" problem
            if ((isCvTech && isJobMedical) || (isCvMedical && isJobTech))
                return -0.2f; // TEST: 20% penalty for completely different domains

            return 0f; // No bonus/penalty - neutral case
        }

        /// <summary>
        /// FALLBACK: Generate simple embedding when API quota exceeded
        /// TODO: Replace with proper offline embedding model or better fallback logic
        /// </summary>
        private float[] GetFallbackEmbedding(string text)
        {
            _logger.LogWarning("Using FALLBACK embedding generation - results will be less accurate!");
            
            // Simple hash-based embedding generation for testing
            var hash = text.GetHashCode();
            var random = new Random(Math.Abs(hash));
            
            // Generate a normalized 768-dimension vector (typical for embeddings)
            var embedding = new float[768];
            float magnitude = 0f;
            
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = (float)(random.NextDouble() * 2 - 1); // -1 to 1
                magnitude += embedding[i] * embedding[i];
            }
            
            // Normalize the vector
            magnitude = MathF.Sqrt(magnitude);
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= magnitude;
            }
            
            return embedding;
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
