using System.Collections.Generic;

namespace JBDH.Models.User
{
    public class Manager : User
    {
        // Organizations managed by this manager
        public new ICollection<User> OwnedUsers { get; set; } = new List<User>();
        public ICollection<Recruiter> OwnedRecruiters { get; set; } = new List<Recruiter>();
    }
}
