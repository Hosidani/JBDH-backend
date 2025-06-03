using System.Collections.Generic;

namespace JBDH.Models
{
    public class Manager : User
    {
        // Organizations managed by this manager
        public ICollection<Organisation> ManagedOrganisations { get; set; } = new List<Organisation>();
    }
}
