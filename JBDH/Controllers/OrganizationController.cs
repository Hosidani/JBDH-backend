using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace JBDH.Controllers
{
    [Authorize(Roles = "Manager")] // Restrict access to users with the "Manager" role
    public class OrganizationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrganizationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult RegisterOrganization()
        {
            return View(); // Return a view for registering an organization
        }

        [HttpPost]
        public async Task<IActionResult> RegisterOrganization(string name, string description)
        {
            if (string.IsNullOrEmpty(name))
            {
                ViewData["ErrorMessage"] = "Organization name is required.";
                return View();
            }

            try
            {
                // Get the current manager
                var managerUsername = User.Identity?.Name;
                var manager = await _context.Managers.FirstOrDefaultAsync(m => m.Username == managerUsername);

                if (manager == null)
                {
                    ViewData["ErrorMessage"] = "You must be a manager to register an organization.";
                    return View();
                }

                // Create the organization
                var organization = new Organization
                {
                    Name = name,
                    Description = description,
                    Managers = new List<Manager> { manager } // Assign the current manager
                };

                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync();

                ViewData["SuccessMessage"] = "Organization registered successfully!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddManager(int organizationId, string managerUsername)
        {
            if (string.IsNullOrEmpty(managerUsername))
            {
                ViewData["ErrorMessage"] = "Manager username is required.";
                return RedirectToAction("Details", new { id = organizationId });
            }

            try
            {
                var organization = await _context.Organizations
                    .Include(o => o.Managers)
                    .FirstOrDefaultAsync(o => o.Id == organizationId);

                if (organization == null)
                {
                    ViewData["ErrorMessage"] = "Organization not found.";
                    return RedirectToAction("Index");
                }

                var managerToAdd = await _context.Managers.FirstOrDefaultAsync(m => m.Username == managerUsername);
                if (managerToAdd == null)
                {
                    ViewData["ErrorMessage"] = "Manager not found.";
                    return RedirectToAction("Details", new { id = organizationId });
                }

                if (organization.Managers.Contains(managerToAdd))
                {
                    ViewData["ErrorMessage"] = "This manager is already part of the organization.";
                    return RedirectToAction("Details", new { id = organizationId });
                }

                organization.Managers.Add(managerToAdd);
                await _context.SaveChangesAsync();

                ViewData["SuccessMessage"] = "Manager added to the organization successfully!";
                return RedirectToAction("Details", new { id = organizationId });
            }
            catch
            {
                ViewData["ErrorMessage"] = "An error occurred while adding the manager to the organization.";
                return RedirectToAction("Details", new { id = organizationId });
            }
        }
    }
}
