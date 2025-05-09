namespace JBDH.Services
{
    public static class PrintJobDetailsService
    {
        
        public static void PrintJobDetails(Dictionary<string, object> job)
                {
                    LoggingConfigService.ReturnInfo($"Title: {job.GetValueOrDefault("headline", "N/A")}");
                    LoggingConfigService.ReturnInfo($"Company: {((Dictionary<string, object>)job.GetValueOrDefault("employer", new Dictionary<string, object>()))
                        .GetValueOrDefault("name", "N/A")}");
                    LoggingConfigService.ReturnInfo($"Location: {((Dictionary<string, object>)job.GetValueOrDefault("workplace_address", new Dictionary<string, object>()))
                        .GetValueOrDefault("municipality", "N/A")}");
                    LoggingConfigService.ReturnInfo($"Job ID: {job.GetValueOrDefault("id", "N/A")}");
                    var descriptionDict = job.GetValueOrDefault("description", null) as Dictionary<string, object>;
                    var description = descriptionDict?.GetValueOrDefault("text", "")?.ToString();
                    LoggingConfigService.ReturnInfo($"Description preview: {description?.Substring(0, Math.Min(200, description.Length))}...");
                }
    }
}