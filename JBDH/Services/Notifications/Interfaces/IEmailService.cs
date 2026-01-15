using JBDH.Models;

namespace JBDH.Services.Notifications
{
    public interface IEmailService
    {
        Task<string> SendVerificationEmailAsync(string toEmail, string verificationToken);
        Task<string> SendEmailAsync(string to, string subject, string body);
    }

}
