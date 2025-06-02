using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace JBDH.Controllers
{
    [Authorize(Roles = "Manager")] // Restrict access to users with the "Manager" role
    public class OrganisationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrganisationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult RegisterOrganisation()
        {
            return View(); // Return a view for registering an organisation
        }

        [HttpPost]
        public async Task<IActionResult> RegisterOrganisation(string name, string description)
        {
            if (string.IsNullOrEmpty(name))
            {
                ViewData["ErrorMessage"] = "Organisation name is required.";
                return View();
            }

            try
            {
                // Get the current manager
                var managerUsername = User.Identity?.Name;
                var manager = await _context.Managers.FirstOrDefaultAsync(m => m.Username == managerUsername);

                if (manager == null)
                {
                    ViewData["ErrorMessage"] = "You must be a manager to register an organisation.";
                    return View();
                }

                // Create the organisation
                var organisation = new Organisation
                {
                    Name = name,
                    Managers = new List<Manager> { manager } // Assign the current manager
                };

                _context.Organisations.Add(organisation);
                await _context.SaveChangesAsync();

                ViewData["SuccessMessage"] = "Organisation registered successfully!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddManager(int organisationId, string managerUsername)
        {
            if (string.IsNullOrEmpty(managerUsername))
            {
                ViewData["ErrorMessage"] = "Manager username is required.";
                return RedirectToAction("Details", new { id = organisationId });
            }

            try
            {
                var organisation = await _context.Organisations
                    .Include(o => o.Managers)
                    .FirstOrDefaultAsync(o => o.Id == organisationId);

                if (organisation == null)
                {
                    ViewData["ErrorMessage"] = "Organisation not found.";
                    return RedirectToAction("Index");
                }

                var managerToAdd = await _context.Managers.FirstOrDefaultAsync(m => m.Username == managerUsername);
                if (managerToAdd == null)
                {
                    ViewData["ErrorMessage"] = "Manager not found.";
                    return RedirectToAction("Details", new { id = organisationId });
                }

                if (organisation.Managers.Contains(managerToAdd))
                {
                    ViewData["ErrorMessage"] = "This manager is already part of the organisation.";
                    return RedirectToAction("Details", new { id = organisationId });
                }

                organisation.Managers.Add(managerToAdd);
                await _context.SaveChangesAsync();

                ViewData["SuccessMessage"] = "Manager added to the organisation successfully!";
                return RedirectToAction("Details", new { id = organisationId });
            }
            catch
            {
                ViewData["ErrorMessage"] = "An error occurred while adding the manager to the organisation.";
                return RedirectToAction("Details", new { id = organisationId });
            }
        }
    }
}
