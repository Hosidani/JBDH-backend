using System.Security.Claims;
using System.Threading.Tasks;
using JBDH.Models.User;
using JBDH.Models.ViewModels;
using JBDH.Models.Services;
using JBDH.Models.Core;

namespace JBDH.Services.UserManagement
{
    public interface IProfileService
    {
    Task<User?> GetUserByIdAsync(int userId);

    // New: Get current user ID from ClaimsPrincipal
    int? GetCurrentUserId(ClaimsPrincipal user);

    // New: Build profile view model for another user
    Task<ProfileViewModel?> GetProfileViewModelAsync(int currentUserId, int targetUserId);

    // New: Build profile view model for self
    Task<ProfileViewModel> GetMyProfileViewModelAsync(int userId);

    // New: Send a friend request
    Task<ServiceResult> SendFriendRequestAsync(int requesterId, int addresseeId);
    }
}
