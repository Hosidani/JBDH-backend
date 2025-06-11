using System.Threading.Tasks;
using JBDH.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JBDH.Services
{
    public interface IUserRegistrationService
    {
        Task<(bool Success, string ErrorMessage)> RegisterUserAsync(RegisterUserViewModel model, HttpContext httpContext, IUrlHelper urlHelper);
    }
}