using JBDH.Models;
public class Friendship
{
    public int Id { get; set; }
    public int RequesterId { get; set; }
    public int AddresseeId { get; set; }
    public FriendshipStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }

    public User Requester { get; set; }
    public User Addressee { get; set; }
}