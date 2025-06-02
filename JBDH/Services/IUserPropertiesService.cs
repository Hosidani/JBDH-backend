using JBDH.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace JBDH.Services
{
    public interface IUserPropertiesService
    {
        Task<User> GetCurrentUserAsync();
        Task<bool> UpdateEmailAsync(string newEmail);
        Task<bool> UpdateOpenForHiringAsync(bool openForHiring);
        Task<bool> UploadCVAsync(IFormFile cvFile);
        Task<bool> UpdateDeveloperModeAsync(bool developerMode);
        Task<(bool Success, string ErrorMessage)> ChangeCVAndAnalyzeAsync(IFormFile cvFile, int userId);
    }
}
