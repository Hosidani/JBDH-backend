namespace JBDH.Models.DTOs
{
    public class OrganisationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Manager" eller bara tillhörig medlem
    }
}