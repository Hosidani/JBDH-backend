using System.Threading.Tasks;

public interface IRegisterOrganisationService
{
    Task<RegisterOrganisationResult> RegisterAsync(string organisationName, string managerUsername);
    
}