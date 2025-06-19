using System.Threading.Tasks;

namespace JBDH.Services.Organisation
{
    public interface IRegisterOrganisationService
    {
        Task<RegisterOrganisationResult> RegisterAsync(string organisationName, string managerUsername);
    }
}