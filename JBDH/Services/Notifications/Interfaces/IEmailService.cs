using JBDH.Models;

namespace JBDH.Services.Notifications
{
    public interface IEmailService
    {
    Task SendEmailAsync(string to, string subject, string body);
    }

}
