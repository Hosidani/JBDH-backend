using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace JBDH.Services.Notifications
{
    public class EmailService : IEmailService
    {
        private readonly EmailClient _emailClient;
        private readonly string? _senderAddress;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            var connectionString = config["EmailCommunicationService"];
            _senderAddress = config["EmailSenderAddress"];
            _logger = logger;
            _emailClient = new EmailClient(connectionString);
            if (string.IsNullOrWhiteSpace(_senderAddress))
                throw new ArgumentException("EmailSenderAddress is not configured in appsettings.");
        }

        public async Task<string> SendVerificationEmailAsync(string toEmail, string verificationToken)
        {
            var subject = "Verify your account";
            var verificationUrl = $"https://yourdomain.com/verify?token={verificationToken}";
            var htmlContent = $"<p>Click the link to verify your account:</p><a href='{verificationUrl}'>{verificationUrl}</a>";
            return await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task<string> SendEmailAsync(string to, string subject, string body)
        {
            var emailContent = new EmailContent(subject)
            {
                Html = body
            };

            var emailRecipients = new EmailRecipients(new[] { new EmailAddress(to) });
            var emailMessage = new EmailMessage(_senderAddress, emailRecipients, emailContent);

            try
            {
                var response = await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
                if (response.Value.Status == EmailSendStatus.Succeeded)
                {
                    _logger.LogInformation($"Email sent to {to}");
                    return "Success";
                }
                else
                {
                    _logger.LogError($"Failed to send email: {response.Value.Status}");
                    return $"Failed: {response.Value.Status}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception sending email to {to}");
                return $"Error: {ex.Message}";
            }
        }
    }
}