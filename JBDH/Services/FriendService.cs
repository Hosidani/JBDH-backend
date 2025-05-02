using JBDH.Models;
using JBDH.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class FriendService : IFriendService
{
    private readonly ApplicationDbContext _context;

    public FriendService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> SendFriendRequestAsync(int requesterId, int addresseeId)
    {
        if (requesterId == addresseeId)
            return false;

        bool exists = await _context.Friendships.AnyAsync(f =>
            (f.RequesterId == requesterId && f.AddresseeId == addresseeId) ||
            (f.RequesterId == addresseeId && f.AddresseeId == requesterId) && f.Status == FriendshipStatus.Pending);

        if (exists)
            return false;

        var friendship = new Friendship
        {
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = FriendshipStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        _context.Friendships.Add(friendship);
        await _context.SaveChangesAsync();
        return true;
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

    public async Task<List<Friendship>> GetPendingRequestsAsync(int userId)
    {
        return await _context.Friendships
            .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
            .ToListAsync();
    }
}
