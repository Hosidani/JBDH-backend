using System.ComponentModel.DataAnnotations;

namespace JBDH.Models.DTOs
{
    public class DeleteAccountRequest
    {
        [Required(ErrorMessage = "Password is required for account deletion")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please type 'DELETE MY ACCOUNT' to confirm")]
        public string ConfirmationText { get; set; } = string.Empty;
    }
}