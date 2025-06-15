using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JBDH.Models;

public class RegisterOrganisationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RegisterOrganisationService : IRegisterOrganisationService
{
    private readonly ApplicationDbContext _context;

    public RegisterOrganisationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RegisterOrganisationResult> RegisterAsync(string organisationName, string managerUsername)
    {
        if (string.IsNullOrWhiteSpace(organisationName))
            return new RegisterOrganisationResult { Success = false, ErrorMessage = "Organisation name cannot be empty." };

        var manager = await _context.Users.FirstOrDefaultAsync(u => u.Username == managerUsername);
        if (manager == null)
            return new RegisterOrganisationResult { Success = false, ErrorMessage = "Manager user not found." };

        // Optional: Check if user is allowed to register an organisation
        var allowedRoles = new[] { "Admin", "Manager" };
        bool hasAllowedRole = manager.Roles.Any(r => allowedRoles.Contains(r.Role));
        if (!hasAllowedRole)
            return new RegisterOrganisationResult { Success = false, ErrorMessage = "User does not have permission to register an organisation." };

        // Optional: Check if organisation already exists
        var exists = await _context.Organisations.AnyAsync(o => o.Name == organisationName);
        if (exists)
            return new RegisterOrganisationResult { Success = false, ErrorMessage = "Organisation already exists." };

        var organisation = new Organisation
        {
            Name = organisationName,
        };

        _context.Organisations.Add(organisation);
        await _context.SaveChangesAsync();

        // Optionally, assign manager as owner or link user to organisation here

        return new RegisterOrganisationResult { Success = true };
    }
}