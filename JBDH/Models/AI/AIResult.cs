namespace JBDH.Models.AI
{
    public class AIResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Response { get; set; }
        public static AIResult SuccessResult(string response) => new AIResult { Success = true, Response = response };
        public static AIResult ErrorResult(string errorMessage) => new AIResult { Success = false, ErrorMessage = errorMessage };
    }
}