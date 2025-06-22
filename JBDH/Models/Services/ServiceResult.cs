namespace JBDH.Models.Services
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public static ServiceResult SuccessResult() => new ServiceResult { Success = true };
        public static ServiceResult Failure(string error) => new ServiceResult { Success = false, ErrorMessage = error };
    }
}