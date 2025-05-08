namespace JBDH.Models
{
    public class RegisterUserViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string Email { get; set; }
        public IFormFile CVFile { get; set; }
        public bool OpenForHiring { get; set; }
    }
}