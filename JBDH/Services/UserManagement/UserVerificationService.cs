using JBDH.Models;
using JBDH.Services; // For IEmailService (adjust if your interface is elsewhere)
using Microsoft.AspNetCore.Mvc; // For IUrlHelper
using Microsoft.AspNetCore.Http; // For HttpRequest if needed
using Microsoft.EntityFrameworkCore; // For EF Core methods

public class VerificationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UserVerificationService : IUserVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public UserVerificationService(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public VerificationResult VerifyEmail(string token)
    {
        var emailVerificationToken = _context.EmailVerificationTokens
            .FirstOrDefault(t => t.Token == token && !t.IsUsed);

        if (emailVerificationToken == null || emailVerificationToken.ExpirationDate <= DateTime.UtcNow)
        {
            return new VerificationResult
            {
                Success = false,
                ErrorMessage = "The verification link is invalid or has expired. Please request a new verification email."
            };
        }

        emailVerificationToken.IsUsed = true;
        var user = _context.Users.FirstOrDefault(u => u.Id == emailVerificationToken.UserId);
        if (user != null)
        {
            // If you have an EmailConfirmed property, set it here
            // user.EmailConfirmed = true;
        }
        _context.SaveChanges();

        return new VerificationResult { Success = true };
    }

    public async Task<VerificationResult> ResendVerificationEmailAsync(string email, IUrlHelper urlHelper, HttpRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return new VerificationResult { Success = false, ErrorMessage = "No user found with that email address." };

        var token = Guid.NewGuid().ToString();
        var emailVerificationToken = new EmailVerificationToken
        {
            Token = token,
            UserId = user.Id,
            ExpirationDate = DateTime.UtcNow.AddDays(1),
            IsUsed = false
        };
        _context.EmailVerificationTokens.Add(emailVerificationToken);
        await _context.SaveChangesAsync();

        var verificationLink = urlHelper.Action("VerifyEmail", "Register", new { token }, request.Scheme);
        await _emailService.SendEmailAsync(user.Email, "Verify your email", $"Please verify your email by clicking this link: {verificationLink}");

        return new VerificationResult { Success = true };
    }
}