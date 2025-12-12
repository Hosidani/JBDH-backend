using System.ComponentModel.DataAnnotations.Schema;
using JBDH.Models.User;
using Newtonsoft.Json;

namespace JBDH.Models.AI
{
    public class UserAIResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? ResponseText { get; set; } // Store the raw JSON
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public CVAnalysisResult? AnalysisResult
        {
            get
            {
                if (string.IsNullOrEmpty(ResponseText)) return null;
                try
                {
                    return JsonConvert.DeserializeObject<CVAnalysisResult>(ResponseText);
                }
                catch
                {
                    return null;
                }
            }
        }

        [JsonIgnore]
        public required JBDH.Models.User.User User { get; set; }
    }
}