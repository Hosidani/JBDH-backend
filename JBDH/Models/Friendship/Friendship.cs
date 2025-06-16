using JBDH.Models;
using JBDH.Models.User;
using System;
using System.ComponentModel.DataAnnotations.Schema;
// ReSharper disable UnusedAutoPropertyAccessor.Global
public class Friendship
{
    public int Id { get; set; }

    public int RequesterId { get; set; }

    [ForeignKey(nameof(RequesterId))]
    public required User Requester { get; set; }

    public int AddresseeId { get; set; }

    [ForeignKey(nameof(AddresseeId))]
    public required User Addressee { get; set; }

    public FriendshipStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
}
