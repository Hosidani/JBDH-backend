using JBDH.Models.User;
using JBDH.Models.Friendship;
using FriendshipModel = JBDH.Models.Friendship.Friendship;
using JBDH.Models.Services;

namespace JBDH.Services.Friendship
{
    public interface IFriendService
    {
        // Methods for managing friendships
        Task<ServiceResult> SendFriendRequestAsync(int requesterId, int addresseeId);
        Task<bool> AcceptFriendRequestAsync(int requestId);
        Task<bool> DeclineFriendRequestAsync(int requestId); // <-- Add this line
        Task<List<User>> GetFriendsAsync(int userId);
        Task<List<FriendshipModel>> GetPendingRequestsAsync(int userId);
        Task<bool> AreFriendsAsync(int userId1, int userId2);
    }
}