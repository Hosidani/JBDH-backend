using JBDH.Services.Configuration;

namespace JBDH.Services.Core
{
    public class DeleteJsonFileService
    {
        public static void DeleteJsonFile()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "jobs.json");
            if (File.Exists(filePath))
            {
                try
                {
                    // Try to remove ReadOnly attribute if set
                    File.SetAttributes(filePath, FileAttributes.Normal);
                }
                catch
                {
                    // Ignore if cannot set attributes
                }
                try
                {
                    File.Delete(filePath);
                    LoggingConfigService.ReturnInfo($"Deleted JSON file: {filePath}");
                }
                catch
                {
                    // Ignore if cannot delete
                    LoggingConfigService.ReturnInfo($"Could not delete JSON file: {filePath} (may not exist or be locked)");
                }
            }
            else
            {
                LoggingConfigService.ReturnInfo($"JSON file not found: {filePath}");
            }
        }
    }
}