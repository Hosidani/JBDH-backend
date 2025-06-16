using System.ComponentModel.DataAnnotations;

namespace JBDH.Models
{
    public class SmtpSettings
    {
        [Required]
        public string Host { get; set; } = "smtp.gmail.com";

        [Range(1, 65535)]
        public int Port { get; set; } = 587;

        public bool EnableSSL { get; set; } = true;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
