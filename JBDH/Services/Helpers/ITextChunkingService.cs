namespace JBDH.Services.Helpers
{
    public interface ITextChunkingService
    {
        /// <summary>
        /// Chunks text into smaller pieces with specified maximum words and overlap
        /// </summary>
        /// <param name="text">The text to chunk</param>
        /// <param name="maxWords">Maximum number of words per chunk (default: 200)</param>
        /// <param name="overlap">Number of words to overlap between chunks (default: 40)</param>
        /// <returns>List of text chunks</returns>
        List<string> ChunkText(string text, int maxWords = 200, int overlap = 40);

        /// <summary>
        /// Specifically chunks job ad text with optimized settings for job requirements analysis
        /// </summary>
        /// <param name="jobAdText">The job advertisement text to chunk</param>
        /// <returns>List of job ad text chunks</returns>
        List<string> ChunkJobAd(string jobAdText);
    }
}
