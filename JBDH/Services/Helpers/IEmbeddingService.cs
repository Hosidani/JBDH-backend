namespace JBDH.Services.Helpers
{
    /// <summary>
    /// Service for generating text embeddings using OpenAI or Gemini APIs with RAG optimization
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generate embedding vector for a given text with optional task type for RAG optimization
        /// </summary>
        /// <param name="text">Text to embed</param>
        /// <param name="taskType">Task type for Gemini (RETRIEVAL_DOCUMENT, RETRIEVAL_QUERY, SEMANTIC_SIMILARITY)</param>
        /// <returns>Float array representing the embedding vector</returns>
        Task<float[]> GetEmbeddingAsync(string text, string taskType = "RETRIEVAL_DOCUMENT");

        /// <summary>
        /// Generate embeddings for multiple texts in a batch with task type support
        /// </summary>
        /// <param name="texts">List of texts to embed</param>
        /// <param name="taskType">Task type for Gemini embeddings</param>
        /// <returns>List of embedding vectors</returns>
        Task<List<float[]>> GetEmbeddingsBatchAsync(List<string> texts, string taskType = "RETRIEVAL_DOCUMENT");

        /// <summary>
        /// Calculate cosine similarity between two embedding vectors
        /// </summary>
        /// <param name="vec1">First embedding vector</param>
        /// <param name="vec2">Second embedding vector</param>
        /// <returns>Similarity score (0-1)</returns>
        float CalculateSimilarity(float[] vec1, float[] vec2);

        /// <summary>
        /// Compare CV skills with job requirements using embeddings
        /// </summary>
        /// <param name="cvRequirements">List of CV requirements/skills</param>
        /// <param name="jobRequirements">List of job requirements</param>
        /// <returns>Skills match percentage and detailed matches</returns>
        Task<(float skillsMatchPercentage, List<object> detailedMatches)> CompareSkillsWithEmbeddingsAsync(List<JBDH.Models.AI.Requirement> cvRequirements, List<JBDH.Models.AI.Requirement> jobRequirements);

        /// <summary>
        /// Calculate profession compatibility score using embeddings
        /// </summary>
        /// <param name="cvProfession">CV profession information</param>
        /// <param name="jobProfession">Job profession requirements</param>
        /// <returns>Profession match score (0-1)</returns>
        Task<float> CalculateProfessionScoreWithEmbeddingsAsync(JBDH.Models.AI.Profession? cvProfession, JBDH.Models.AI.Profession? jobProfession);
    }
}
