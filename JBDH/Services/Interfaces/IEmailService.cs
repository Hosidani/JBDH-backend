using JBDH.Models;

namespace JBDH.Services
{
    public interface IEmailService
    {
    Task SendEmailAsync(string to, string subject, string body);
    }

}
