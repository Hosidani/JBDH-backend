using JBDH.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

public class Friendship
{
    public int Id { get; set; }

    public int RequesterId { get; set; }

    [ForeignKey(nameof(RequesterId))]
    public User Requester { get; set; }

    public int AddresseeId { get; set; }

    [ForeignKey(nameof(AddresseeId))]
    public User Addressee { get; set; }

    public FriendshipStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
}
