namespace JBDH.Services.Helpers
{
    /// <summary>
    /// Service for generating text embeddings using DeepSeek API
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generate embedding vector for a given text
        /// </summary>
        /// <param name="text">Text to embed</param>
        /// <returns>Float array representing the embedding vector (1536 dimensions)</returns>
        Task<float[]> GetEmbeddingAsync(string text);

        /// <summary>
        /// Generate embeddings for multiple texts in a batch
        /// </summary>
        /// <param name="texts">List of texts to embed</param>
        /// <returns>List of embedding vectors</returns>
        Task<List<float[]>> GetEmbeddingsBatchAsync(List<string> texts);

        /// <summary>
        /// Calculate cosine similarity between two embedding vectors
        /// </summary>
        /// <param name="vec1">First embedding vector</param>
        /// <param name="vec2">Second embedding vector</param>
        /// <returns>Similarity score (0-1)</returns>
        float CalculateSimilarity(float[] vec1, float[] vec2);
    }
}
