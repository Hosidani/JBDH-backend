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
using System.Text.Json;
using JBDH.Services.Configuration;

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
            var jobApplications = await (
                from app in _context.JobApplications
                join job in _context.Jobs on app.JobId equals job.JobId
                where app.UserId == userId
                select new JBDH.Models.ViewModels.ProfileViewModel.JobApplicationViewModel
                {
                    ApplicationId = app.ApplicationId,
                    JobId = app.JobId,
                    JobTitle = job.Title,
                    JobUrl = job.WebpageUrl,
                    JobDescription = job.Description,
                    Notes = app.Notes,
                    AppliedAt = app.AppliedAt
                }
            ).ToListAsync();

            var userAIResponse = await _userAIResponseService.GetLatestUserAIResponseAsync(user.Id);
            LoggingConfigService.ReturnDebug($"UserAIResponse for user {user.Id} retrieved: {userAIResponse?.ResponseText ?? "No response"}");
            var model = new ProfileViewModel
            {
                User = user,
                IsFriend = false,
                Organisation = null,
                UserAIResponse = userAIResponse,
                Friends = friends,
                PendingFriendRequests = pendingRequests,
                JobApplications = jobApplications
            };

            // Example using System.Text.Json
            if (userAIResponse != null && !string.IsNullOrEmpty(userAIResponse.ResponseText))
            {
                try
                {
                    model.CVAnalysisResult = JsonSerializer.Deserialize<CVAnalysisResult>(userAIResponse.ResponseText);
                }
                catch
                {
                    model.CVAnalysisResult = null; // or handle error
                }
            }
            else
            {
                model.CVAnalysisResult = null;
            }

            return model;
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