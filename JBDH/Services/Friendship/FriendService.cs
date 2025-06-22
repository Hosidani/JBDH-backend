using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.User;
using JBDH.Models.Friendship;
using JBDH.Models.Services;
using Microsoft.EntityFrameworkCore;
using FriendshipModel = JBDH.Models.Friendship.Friendship;

namespace JBDH.Services.Friendship
{
    public class FriendService : IFriendService
    {
        private readonly ApplicationDbContext _context;

        public FriendService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult> SendFriendRequestAsync(int requesterId, int addresseeId)
        {
            if (requesterId == addresseeId)
                return ServiceResult.Failure("You cannot send a friend request to yourself.");

            bool exists = await _context.Friendships.AnyAsync(f =>
                (f.RequesterId == requesterId && f.AddresseeId == addresseeId) ||
                (f.RequesterId == addresseeId && f.AddresseeId == requesterId) && f.Status == FriendshipStatus.Pending);

            if (exists)
                return ServiceResult.Failure("A pending friend request already exists.");

            var requester = await _context.Users.FindAsync(requesterId);
            var addressee = await _context.Users.FindAsync(addresseeId);

            if (requester == null || addressee == null)
                return ServiceResult.Failure("User not found.");

            var friendship = new FriendshipModel
            {
                RequesterId = requesterId,
                Requester = requester,
                AddresseeId = addresseeId,
                Addressee = addressee,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();
            return ServiceResult.SuccessResult();
        }



        public async Task<bool> AreFriendsAsync(int requesterId, int addresseeId)
        {
            if (requesterId == addresseeId)
                return false;
                
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == requesterId && f.AddresseeId == addresseeId) ||
                    (f.RequesterId == addresseeId && f.AddresseeId == requesterId) && 
                    f.Status == FriendshipStatus.Friends);

            return friendship != null;
        }


        public async Task<bool> AcceptFriendRequestAsync(int requestId)
        {
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.Id == requestId && f.Status == FriendshipStatus.Pending);

            if (friendship == null)
                return false;

            friendship.Status = FriendshipStatus.Friends;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<User>> GetFriendsAsync(int userId)
        {
            var friendships = await _context.Friendships
                .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == FriendshipStatus.Friends)
                .ToListAsync();

            var friendIds = friendships
                .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
                .Distinct()
                .ToList();

            var friends = await _context.Users
                .Where(u => friendIds.Contains(u.Id))
                .ToListAsync();

            return friends;
        }

        public async Task<List<FriendshipModel>> GetPendingRequestsAsync(int userId)
        {
            return await _context.Friendships
                .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
                .ToListAsync();
        }
    }
}
