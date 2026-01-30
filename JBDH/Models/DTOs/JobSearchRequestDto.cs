namespace JBDH.Models.DTOs
{
    public class JobSearchRequestDto
    {
        public string? Location { get; set; }
        public string? Description { get; set; }
        public string? Competence { get; set; }
        public string? WorkArea { get; set; }
        public string? Roles { get; set; }
    }
}