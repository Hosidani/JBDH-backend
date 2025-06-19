using JBDH.Models;
using JBDH.Models.User;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace JBDH.Services.UserManagement
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
