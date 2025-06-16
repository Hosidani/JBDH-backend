using JBDH.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace JBDH.Services
{
    public interface IUserPropertiesService
    {
        Task<User?> GetCurrentUserAsync();
        Task<bool> UpdateEmailAsync(string newEmail);
        Task<bool> UpdateOpenForHiringAsync(bool openForHiring);
        Task<ServiceResult> UploadCVAsync(IFormFile cvFile);
        Task<bool> UpdateDeveloperModeAsync(bool developerMode);
        Task<ServiceResult> ChangeCVAndAnalyzeAsync(IFormFile cvFile, int userId);
    }
}
