using System.Collections.Generic;

namespace JBDH.Models.User
{
    public class Recruiter : User
    {
        public new ICollection<User> OwnedUsers { get; set; } = new List<User>();
    }
}