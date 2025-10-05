using Microsoft.AspNetCore.Http;

namespace JBDH.Services.Helpers
{
    public static class FileHelper
    {
        public static async Task<string?> SaveUploadedFileAsync(IFormFile file, string webRootPath, string subFolder = "uploads")
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(webRootPath, subFolder);
            Directory.CreateDirectory(uploadsFolder);
            var savedPath = Path.Combine(uploadsFolder, Path.GetFileName(file.FileName));
            using (var stream = new FileStream(savedPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return savedPath;
        }
    }
}