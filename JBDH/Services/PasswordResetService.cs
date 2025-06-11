using JBDH.Models;
using JBDH.Services;
using Microsoft.EntityFrameworkCore;
public class PasswordResetService : IPasswordResetService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PasswordResetService(ApplicationDbContext context, IEmailService emailService, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || user.IsBanned) return;

        var token = Guid.NewGuid().ToString();
        var resetEntry = new PasswordReset
        {
            UserId = user.Id,
            Token = token,
            ExpirationDate = DateTime.UtcNow.AddHours(1)
        };

        _context.PasswordResets.Add(resetEntry);
        await _context.SaveChangesAsync();

        var request = _httpContextAccessor.HttpContext.Request;
        var resetLink = $"{request.Scheme}://{request.Host}/Login/ResetPassword?token={token}";
        var body = $"<p>Reset your password by clicking <a href='{resetLink}'>here</a>.</p>";

        await _emailService.SendEmailAsync(user.Email, "Reset Your Password", body);
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var entry = await _context.PasswordResets
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Token == token && p.ExpirationDate > DateTime.UtcNow);

        // Here is the code example if we decide to keep a history of all password reset requests (for auditing, debugging, or security monitoring).
        /*
        if (entry.IsUsed)
            return false; 
        entry.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        entry.IsUsed = true;
        */
        
        //Currently solution: deleting the token after use is secure and prevents replay attacks (someone using the same link again).
        if (entry == null)
            return false;
        entry.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        _context.PasswordResets.Remove(entry);

        await _context.SaveChangesAsync();
        return true;
    }
}
