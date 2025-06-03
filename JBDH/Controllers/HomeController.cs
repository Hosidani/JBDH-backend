using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using JBDH.Services;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using JBDH.Models.ViewModels;
using System.Security.Claims;


namespace JBDH.Controllers
{

    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private readonly DataFlowService _dataFlowService;
        private readonly IRapportUserService _rapportUserService;
        public HomeController(ApplicationDbContext context, IWebHostEnvironment environment, DataFlowService dataFlowService, IRapportUserService rapportUserService)
        {
            _context = context;
            _environment = environment;
            _dataFlowService = dataFlowService;
            _rapportUserService = rapportUserService;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Index()
        {
            var flows = _context.DataFlows.OrderByDescending(f => f.CreatedAt).ToList();
            return View(flows);
        }

        // Skapa nytt inlägg (GET)
        public IActionResult CreateFlow()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateFlow(DataFlowViewModel model)
        {
            // Kontrollera om användaren är inloggad och få user-ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                LoggingConfigService.ReturnError("User is not authenticated.");
                return RedirectToAction("Login", "Account");
            }
            LoggingConfigService.ReturnInfo($"Creating new data flow with title: {model.Content} IN CONTROLLER");
            if (!ModelState.IsValid)
            {
                LoggingConfigService.ReturnError("Model state is invalid.");
                return View(model);
            }
            if (Int32.Parse(userId) > 0)
            {
                if (string.IsNullOrEmpty(model.Content))
                {
                    LoggingConfigService.ReturnError("Content cannot be empty.");
                    ModelState.AddModelError("Content", "Content cannot be empty.");
                    return View();
                }
                LoggingConfigService.ReturnInfo($"Creating new data flow with title: {model.Content} IN CONTROLLER");
                await _dataFlowService.CreateAsync(model, Int32.Parse(userId));
            }
            return RedirectToAction("Index");
        }

        public IActionResult UserPosts(int userId)
        {
            var userPosts = _context.DataFlows.Where(df => df.UserId == userId).ToList();
            return View(userPosts);
        }

        [HttpGet]
        public IActionResult GeneralUswerFlow()
        {
            // Get all posts from the database
            var allPosts = _dataFlowService.GetAll();

            // Return the view with the posts
            return View(allPosts);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                Title = "Unexpected Error",
                Message = "An unexpected error occurred. Please try again later."
            });

        }

        [HttpGet]
        public IActionResult Settings()
        {
            return View("~/Views/Settings/Settings.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> ReportDataFlow(int reportedUserId, int dataFlowId, string dataFlowText, string reason)
        {
            // Hämta reporterUserId från inloggad användare
            var reporterClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (reporterClaim == null)
            {
                TempData["Error"] = "User is not authenticated.";
                return RedirectToAction("Login", "Account");
            }
            var reporterUserId = int.Parse(reporterClaim.Value);
            await _rapportUserService.ReportUserAsync(reporterUserId, reportedUserId, reason, dataFlowId, dataFlowText);
            TempData["Message"] = "The post has been reported.";
            return RedirectToAction("Index");
        }
    }
}