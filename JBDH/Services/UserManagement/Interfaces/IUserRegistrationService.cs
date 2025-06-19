using System.Threading.Tasks;
using JBDH.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JBDH.Services.UserManagement
{
    public interface IUserRegistrationService
    {
        Task<ServiceResult> RegisterUserAsync(RegisterUserViewModel model, HttpContext httpContext, IUrlHelper urlHelper);
    }
}