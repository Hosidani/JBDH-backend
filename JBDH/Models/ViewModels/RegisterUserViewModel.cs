// DTO för API/React
namespace JBDH.Models.DTOs
{
    public class RegisterUserDto
    {
        public string? Password { get; set; }
        public string? Email { get; set; }
        public string? Location { get; set; }
        public string? Name { get; set; }
        public string? DesiredEmploymentType { get; set; }
        public string? DesiredWorkMode { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? TravelAvailability { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public string? DesiredCity { get; set; }
        public string? WorkPlace { get; set; }
        public string? Competencies { get; set; }
        public string? CVPath { get; set; }
        public bool? DeveloperMode { get; set; }
        public bool? Searchable { get; set; }
        public List<string>? SelectedRoles { get; set; }
    }
}
namespace JBDH.Models.ViewModels
{
    public class RegisterUserViewModel
    {
        // Decide which properties are required
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Location { get; set; }
        public IFormFile? CVFile { get; set; }
        public bool Open { get; set; }
        public string PreferredLanguage { get; set; } = "en"; // Lägg till denna rad
        public List<string> AvailableRoles { get; set; } = new List<string> { "Manager", "Recruiter" };
        public List<string> SelectedRoles { get; set; } = new List<string>();
    }
}