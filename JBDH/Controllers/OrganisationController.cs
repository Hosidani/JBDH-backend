using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using JBDH.Services.Organisation;

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
            {
                ViewData["ErrorMessage"] = "You must be logged in to register an organisation.";
                return View();
            }

            var result = await _organisationService.RegisterOrganisationAsync(name, description, managerUsername);

            if (!result.Success)
            {
                ViewData["ErrorMessage"] = result.ErrorMessage;
                return View();
            }

            ViewData["SuccessMessage"] = "Organisation registered successfully!";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> AddManager(int organisationId, string managerUsername)
        {
            var result = await _organisationService.AddManagerAsync(organisationId, managerUsername);

            if (!result.Success)
            {
                ViewData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction("Details", new { id = organisationId });
            }

            ViewData["SuccessMessage"] = "Manager added to the organisation successfully!";
            return RedirectToAction("Details", new { id = organisationId });
        }
    }
}
