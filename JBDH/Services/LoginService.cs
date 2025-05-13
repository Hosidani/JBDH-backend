using BCrypt.Net;
using JBDH.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Services
{
    public class LoginService : ILoginService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Constructor
        public LoginService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Validate user credentials
        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                return false;

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        // Sign in the user
        public async Task SignInUserAsync(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await _httpContextAccessor.HttpContext.SignInAsync("CookieAuth", claimsPrincipal);
        }

        // Register user with organization selection
        public async Task RegisterUserAsync(string username, string password, int? organizationId)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Username = username,
                PasswordHash = passwordHash,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (organizationId.HasValue)
            {
                var organization = await _context.Organizations.Include(o => o.Managers)
                    .FirstOrDefaultAsync(o => o.Id == organizationId.Value);

                if (organization?.Managers != null && organization.Managers.Any())
                {
                    foreach (var manager in organization.Managers)
                    {
                        await SendVerificationEmailAsync(manager.Email, user);
                    }
                }
            }
        }

        private async Task SendVerificationEmailAsync(string managerEmail, User user)
        {
            // Logic to send email to the manager
            var subject = "Verify User for Organization";
            var body = $"Please verify that {user.Username} is part of your organization.";
            // Use an email service to send the email (not implemented here)
            await Task.CompletedTask; // Placeholder for email sending logic
        }
    }
}
