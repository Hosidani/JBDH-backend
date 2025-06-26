using JBDH.Services.Configuration;

namespace JBDH.Services.Core
{
    public class DeleteJsonFileService
    {
        public static void DeleteJsonFile()
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "jobs.json");
                if (File.Exists(filePath))
                {
                    // Remove ReadOnly attribute if set
                    var attributes = File.GetAttributes(filePath);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
                    }
                    File.Delete(filePath);
                    LoggingConfigService.ReturnInfo($"Deleted JSON file: {filePath}");
                }
                else
                {
                    LoggingConfigService.ReturnInfo($"JSON file not found: {filePath}");
                }
            }
            catch (FileNotFoundException)
            {
                LoggingConfigService.ReturnInfo("File not found when attempting to delete jobs.json (handled gracefully).");
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Error deleting JSON file: {e.Message}");
            }
        }
    }
}