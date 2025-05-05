using System.Collections.Generic;

namespace JBDH.Models
{
    public class Manager : User
    {
        // Organizations managed by this manager
        public ICollection<Organization> ManagedOrganizations { get; set; } = new List<Organization>();
    }
}
