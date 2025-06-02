namespace JBDH.Models
{
    public class ManageRolesViewModel
    {
        public int UserId { get; set; }
        public List<string> AvailableRoles { get; set; } = new List<string> { "Manager", "Recruiter" };
        public List<string> SelectedRoles { get; set; } = new List<string>();
    }
}