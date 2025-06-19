using System.Collections.Generic;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.User;
using JBDH.Models.Services;
using OrganisationModel = JBDH.Models.Organisation.Organisation;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Services.Organisation
{
    public class OrganisationService : IOrganisationService
    {
        private readonly ApplicationDbContext _context;

        public OrganisationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult> RegisterOrganisationAsync(string name, string description, string managerUsername)
        {
            if (string.IsNullOrEmpty(name))
                return ServiceResult.Failure("Organisation name is required.");

            var manager = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Username == managerUsername && u.Roles.Any(r => r.Role == "Manager"));

            if (manager == null)
                return ServiceResult.Failure("You must be a manager to register an organisation.");

            var organisation = new OrganisationModel
            {
                Name = name,
                Managers = new List<User> { manager }
            };

            _context.Organisations.Add(organisation);
            await _context.SaveChangesAsync();

            return ServiceResult.SuccessResult();
        }

        public async Task<ServiceResult> AddManagerAsync(int organisationId, string managerUsername)
        {
            if (string.IsNullOrEmpty(managerUsername))
                return ServiceResult.Failure("Manager username is required.");

            var organisation = await _context.Organisations
                .Include(o => o.Managers)
                .FirstOrDefaultAsync(o => o.Id == organisationId);

            if (organisation == null)
                return ServiceResult.Failure("Organisation not found.");

            var managerToAdd = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Username == managerUsername && u.Roles.Any(r => r.Role == "Manager"));

            if (managerToAdd == null)
                return ServiceResult.Failure("Manager not found.");

            if (organisation.Managers.Contains(managerToAdd))
                return ServiceResult.Failure("This manager is already part of the organisation.");

            organisation.Managers.Add(managerToAdd);
            await _context.SaveChangesAsync();

            return ServiceResult.SuccessResult();
        }
    }
}