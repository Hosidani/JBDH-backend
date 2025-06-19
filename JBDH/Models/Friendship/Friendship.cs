using JBDH.Models;
using JBDH.Models.User;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using UserModel = JBDH.Models.User.User;
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace JBDH.Models.Friendship
{
    public class Friendship
    {
        public int Id { get; set; }

        public int RequesterId { get; set; }

        [ForeignKey(nameof(RequesterId))]
        public required UserModel Requester { get; set; }

        public int AddresseeId { get; set; }

        [ForeignKey(nameof(AddresseeId))]
        public required UserModel Addressee { get; set; }

        public FriendshipStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
