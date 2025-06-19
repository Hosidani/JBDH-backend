using JBDH.Services.Configuration;

namespace JBDH.Services.Core
{
    public class DeleteJsonFileService
    {
        public static void DeleteJsonFile()
        {
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "jobs.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    LoggingConfigService.ReturnInfo($"Deleted JSON file: {filePath}");
                }
                else
                {
                    LoggingConfigService.ReturnInfo($"JSON file not found: {filePath}");
                }
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Error deleting JSON file: {e.Message}");
            }
        }
    }
}