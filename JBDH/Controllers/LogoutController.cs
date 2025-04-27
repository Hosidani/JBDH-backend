using JBDH.Models;
using JBDH.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;

namespace JBDH.Controllers;

public class LogoutController : Controller
{
    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.SignOutAsync("CookieAuth");  // Sign out of authentication
        HttpContext.Session.Clear();             // Clear the session
        ViewData["Username"] = null;              // Clear any ViewData
        return RedirectToAction("Index", "Home"); // Redirect to home page
    }
}