using JBDH.Services.Configuration;

namespace JBDH.Services.Helpers
{
    public class TextChunkingService : ITextChunkingService
    {
        public List<string> ChunkText(string text, int maxWords = 200, int overlap = 40)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                LoggingConfigService.ReturnWarning("ChunkText received empty or null text.");
                return new List<string>();
            }

            var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var chunks = new List<string>();
            int i = 0;

            LoggingConfigService.ReturnInfo($"Chunking text with {words.Length} words, maxWords: {maxWords}, overlap: {overlap}");

            while (i < words.Length)
            {
                int end = Math.Min(i + maxWords, words.Length);
                chunks.Add(string.Join(" ", words[i..end]));
                
                if (end == words.Length) break;
                i += (maxWords - overlap);
            }

            LoggingConfigService.ReturnInfo($"Created {chunks.Count} chunks from text.");
            return chunks;
        }

        public List<string> ChunkJobAd(string jobAdText)
        {
            if (string.IsNullOrWhiteSpace(jobAdText))
            {
                LoggingConfigService.ReturnWarning("ChunkJobAd received empty job ad text.");
                return new List<string>();
            }

            LoggingConfigService.ReturnInfo("Chunking job ad text with optimized settings.");
            
            // Use smaller chunks for job ads to better identify specific requirements
            // Use more overlap to ensure requirements aren't split between chunks
            return ChunkText(jobAdText, maxWords: 150, overlap: 50);
        }
    }
}
