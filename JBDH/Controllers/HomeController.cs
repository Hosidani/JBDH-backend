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
using Microsoft.EntityFrameworkCore;


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
            var dataFlows = _dataFlowService.GetAllRecursive();
            var listDataFlowViewModel = new ListDataFlowViewModel { DataFlows = dataFlows };
            return View(listDataFlowViewModel);
        }
        [Authorize]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                LoggingConfigService.ReturnError("User is not authenticated.");
                return RedirectToAction("Login", "Account");
            }
            LoggingConfigService.ReturnInfo($"Deleting data flow for user ID: {userId} IN CONTROLLER");
            _dataFlowService.DeleteDataFlow(id); 
            return RedirectToAction("Index");
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
            var posts = _dataFlowService.GetByUserId(userId);
            return View(posts);
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

            var reporterExists = await _context.Users.AnyAsync(u => u.Id == reporterUserId);
            var reportedExists = await _context.Users.AnyAsync(u => u.Id == reportedUserId);
            if (!reporterExists || !reportedExists)
            {
                // Log or handle error here
                LoggingConfigService.ReturnError($"Reporter or reported user does not exist. Reporter ID: {reporterUserId}, Reported User ID: {reportedUserId}");
                TempData["Error"] = "Reporter or reported user does not exist.";
                return RedirectToAction("Index");
            }

            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, dataFlowId, dataFlowText);
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction("Index");
            }

            TempData["Message"] = "The post has been reported.";
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CommentOnDataFlow(int dataFlowId, string comment)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["Error"] = "Comment cannot be empty.";
                return RedirectToAction("Index");
            }

            await _dataFlowService.AddCommentAsync(dataFlowId, int.Parse(userId), comment);
            return RedirectToAction("Index");
        }
    }
}