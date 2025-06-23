using System.Security.Claims;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.User;
using JBDH.Models.ViewModels;
using JBDH.Models.Services;
using JBDH.Models.Friendship;
using JBDH.Services.Friendship;
using JBDH.Services.AICommunications;
using Microsoft.EntityFrameworkCore;
using JBDH.Models.Job;

namespace JBDH.Services.UserManagement
{
    public class ProfileService : IProfileService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFriendService _friendService;
        private readonly IUserAIResponseService _userAIResponseService;
        public ProfileService(
            ApplicationDbContext context,
            IFriendService friendService,
            IUserAIResponseService userAIResponseService)
        {
            _context = context;
            _friendService = friendService;
            _userAIResponseService = userAIResponseService;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Set<User>()
                .Include(u => u.ReceivedFriendRequests)
                    .ThenInclude(f => f.Requester)
                .Include(u => u.SentFriendRequests)
                    .ThenInclude(f => f.Addressee)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<ProfileViewModel?> GetProfileViewModelAsync(int currentUserId, int targetUserId)
        {
            if (currentUserId == targetUserId)
                return null; // Controller can handle redirect

            var user = await GetUserByIdAsync(targetUserId);
            if (user == null)
                throw new System.Exception("User not found.");

            var isFriend = await _friendService.AreFriendsAsync(currentUserId, targetUserId);

            return new ProfileViewModel
            {
                User = user,
                IsFriend = isFriend,
                Organisation = null
            };
        }

        public async Task<ProfileViewModel> GetMyProfileViewModelAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
                throw new System.Exception("User not found.");

            var friends = user.SentFriendRequests
                .Where(f => f.Status == FriendshipStatus.Accepted || f.Status == FriendshipStatus.Friends)
                .Select(f => f.Addressee)
                .Concat(
                    user.ReceivedFriendRequests
                        .Where(f => f.Status == FriendshipStatus.Accepted || f.Status == FriendshipStatus.Friends)
                        .Select(f => f.Requester)
                )
                .ToList();

            var pendingRequests = user.ReceivedFriendRequests
                .Where(f => f.Status == FriendshipStatus.Pending)
                .ToList();

            // Fetch job applications
            var jobApplications = await _context.Set<JobApplication>()
                .Where(j => j.UserId == userId)
                .ToListAsync();

            return new ProfileViewModel
            {
                User = user,
                IsFriend = false,
                Organisation = null,
                UserAIResponse = await _userAIResponseService.GetLatestUserAIResponseAsync(user.Id),
                Friends = friends,
                PendingFriendRequests = pendingRequests,
                JobApplications = jobApplications
            };
        }

        public int? GetCurrentUserId(ClaimsPrincipal user)
        {
            if (user == null)
                return null;
                
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var id))
                return id;
            return null;
        }

        public async Task<ServiceResult> SendFriendRequestAsync(int requesterId, int addresseeId)
        {
            // Delegate to friend service, or implement logic here
            return await _friendService.SendFriendRequestAsync(requesterId, addresseeId);
        }
    }
}