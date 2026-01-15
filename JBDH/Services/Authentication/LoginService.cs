using JBDH.Services.Configuration;
using JBDH.Models.User;
using JBDH.Models.Core;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JBDH.Models.Authentication;

namespace JBDH.Services.Authentication
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
        public async Task<LoginResult> ValidateUserAsync(string email, string password)
        {
            LoggingConfigService.ReturnInfo("TEST LOG FROM LOGIN SERVICE");
            var result = new LoginResult();
            LoggingConfigService.ReturnInfo($"[DEBUG] email received: '{email}', length: {email?.Length}, hex: {string.Join(" ", System.Text.Encoding.UTF8.GetBytes(email ?? "").Select(b => b.ToString("X2")))}");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                LoggingConfigService.ReturnInfo("[DEBUG] user is null: true");
                result.UserExists = false;
                result.ErrorMessage = "Invalid email or password.";
                return result;
            }
            LoggingConfigService.ReturnInfo("[DEBUG] user is null: false");

            result.UserExists = true;

            if (user.IsBanned)
            {
                result.IsBanned = true;
                result.ErrorMessage = "This account has been banned.";
                return result;
            }

            LoggingConfigService.ReturnInfo($"[DEBUG] About to verify password for user: {user.Email}, hash: {user.PasswordHash}, password: '{password}'");
            bool passwordCheck = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            LoggingConfigService.ReturnInfo($"[DEBUG] Password verify for user {user.Email}: {passwordCheck}, password='{password}', hash='{user.PasswordHash}'");
            result.PasswordCorrect = passwordCheck;
            result.IsSuccess = result.PasswordCorrect;

            if (!result.IsSuccess)
            {
                result.ErrorMessage = "Invalid email or password.";
            }
            return result;
        }


        // Sign in the user
        public async Task SignInUserAsync(string email)
        {
            LoggingConfigService.ReturnInfo($"[SignInUserAsync] Attempting sign-in for {email}");

            if (string.IsNullOrWhiteSpace(email))
            {
                LoggingConfigService.ReturnError("[SignInUserAsync] Email is null or empty.");
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                LoggingConfigService.ReturnError($"[SignInUserAsync] User not found: {email}");
                throw new Exception("User not found");
            }
            if (user.IsBanned)
            {
                LoggingConfigService.ReturnError($"[SignInUserAsync] User is banned: {email}");
                throw new Exception("User is banned");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            if (_httpContextAccessor.HttpContext != null)
            {
                try
                {
                    await _httpContextAccessor.HttpContext.SignInAsync(
                        "CookieAuth",
                        claimsPrincipal,
                        authProperties
                    );
                    LoggingConfigService.ReturnInfo($"[SignInUserAsync] Sign-in succeeded for {email}");
                }
                catch (Exception ex)
                {
                    LoggingConfigService.ReturnError($"[SignInUserAsync] Sign-in failed for {email}: {ex.Message}");
                    throw;
                }
            }
            else
            {
                LoggingConfigService.ReturnError("[SignInUserAsync] HttpContext is null.");
                throw new Exception("HttpContext is null");
            }
        }

        // Register user with organization selection
        public async Task RegisterUserAsync(string email, string password, int? organizationId)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Email = email,
                PasswordHash = passwordHash,
                Location = "", // Set to actual location if available
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (organizationId.HasValue)
            {
                var organization = await _context.Organisations.Include(o => o.Managers)
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
            var body = $"Please verify that {user.Email} is part of your organization.";
            _ = subject;
            _ = body;
            // Use an email service to send the email (not implemented here)
            await Task.CompletedTask; // Placeholder for email sending logic
        }
    }
}
