using Microsoft.AspNetCore.Mvc; // For IUrlHelper

public interface IUserVerificationService
{
    VerificationResult VerifyEmail(string token);
    Task<VerificationResult> ResendVerificationEmailAsync(string email, IUrlHelper urlHelper, HttpRequest request);
}