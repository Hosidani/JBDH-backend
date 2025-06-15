using JBDH.Models;

namespace JBDH.Services
{
    public interface IFriendService
    {
        // Methods for managing friendships
        Task<ServiceResult> SendFriendRequestAsync(int requesterId, int addresseeId);
        Task<bool> AcceptFriendRequestAsync(int requestId);
        Task<List<User>> GetFriendsAsync(int userId);
        Task<List<Friendship>> GetPendingRequestsAsync(int userId);
        Task<bool> AreFriendsAsync(int userId1,int userId2);
    }
}