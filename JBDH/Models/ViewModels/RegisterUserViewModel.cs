namespace JBDH.Models.ViewModels
{
    public class RegisterUserViewModel
    {
        // Decide which properties are required
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public IFormFile? CVFile { get; set; }
        public bool OpenForHiring { get; set; }
        public List<string> AvailableRoles { get; set; } = new List<string> { "Manager", "Recruiter" };
        public List<string> SelectedRoles { get; set; } = new List<string>();
    }
}