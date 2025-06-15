using JBDH.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using JBDH.Services;

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly IFriendService _friendService;
    private readonly IAIResponseService _airesponseService;

    public ProfileService(
        ApplicationDbContext context,
        IFriendService friendService,
        IAIResponseService airesponseService)
    {
        _context = context;
        _friendService = friendService;
        _airesponseService = airesponseService;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Set<User>()
            .Include(u => u.AIResponses)
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

        return new ProfileViewModel
        {
            User = user,
            IsFriend = false,
            Organisation = null,
            UserAIResponse = await _airesponseService.GetLatestAIResponseAsync(user.Id)
        };
    }

    public int? GetCurrentUserId(ClaimsPrincipal user)
    {
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
