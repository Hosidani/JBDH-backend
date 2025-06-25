using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.Services;

namespace JBDH.Services.Organisation
{
    public interface IOrganisationService
    {
        Task<ServiceResult> RegisterOrganisationAsync(string name, string description, string managerUsername);
        Task<ServiceResult> AddManagerAsync(int organisationId, string managerUsername);
    }
}