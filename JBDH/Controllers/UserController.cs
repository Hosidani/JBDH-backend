using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using JBDH.Services;
using BCrypt.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace JBDH.Controllers;

public class UserController : Controller
{
    private readonly ApplicationDbContext _context;


    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }
}
