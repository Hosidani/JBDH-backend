using System.Threading.Tasks;
using JBDH.Models.ViewModels;
using JBDH.Models.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace JBDH.Services.UserManagement
{
    public interface IUserRegistrationService
    {
        Task<ServiceResult> RegisterUserAsync(RegisterUserViewModel model, HttpContext httpContext, IUrlHelper urlHelper);
    }
}