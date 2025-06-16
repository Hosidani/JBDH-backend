using System.Threading.Tasks;
using JBDH.Models;

public interface IOrganisationService
{
    Task<ServiceResult> RegisterOrganisationAsync(string name, string description, string managerUsername);
    Task<ServiceResult> AddManagerAsync(int organisationId, string managerUsername);
}