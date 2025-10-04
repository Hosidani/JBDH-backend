using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JBDH.Services.Organisation;
using JBDH.Models.Core;
using JBDH.Helpers;

namespace JBDH.Controllers
{
    [Authorize(Roles = "Manager")] // Restrict access to users with the "Manager" role
    public class OrganisationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrganisationService _organisationService;

        public OrganisationController(ApplicationDbContext context, IOrganisationService organisationService)
        {
            _context = context;
            _organisationService = organisationService;
        }

        [HttpGet]
        public IActionResult RegisterOrganisation()
        {
            return View(); // Return a view for registering an organisation
        }

        [HttpPost]
        public async Task<IActionResult> RegisterOrganisation(string name, string description)
        {
            var managerUsername = User.Identity?.Name;
            if (string.IsNullOrEmpty(managerUsername))
                return this.ErrorPage("You must be logged in to register an organisation.");

            var result = await _organisationService.RegisterOrganisationAsync(name, description, managerUsername);
            if (!result.Success)
                return this.ErrorPage(result.ErrorMessage ?? "Could not register organisation.");

            return this.SetSuccessAndRedirect("Organisation registered successfully!", "Index", new { controller = "Home" });
        }

        [HttpPost]
        public async Task<IActionResult> AddManager(int organisationId, string managerUsername)
        {
            var result = await _organisationService.AddManagerAsync(organisationId, managerUsername);
            if (!result.Success)
                return this.SetErrorAndRedirect(result.ErrorMessage ?? "Could not add manager.", "Details", new { id = organisationId });

            return this.SetSuccessAndRedirect("Manager added to the organisation successfully!", "Details", new { id = organisationId });
        }
    }
}
