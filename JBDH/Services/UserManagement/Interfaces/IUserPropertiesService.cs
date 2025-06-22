using JBDH.Models.User;
using JBDH.Models.Services;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        Task<List<User>> SearchUsersAsync(string query);
    }
}
