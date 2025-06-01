namespace JBDH.Models
{
    public class AIHelperRequest
    {
        public User? User { get; set; }
        public int CommandId { get; set; }
        public string? CVPath { get; set; }
        public string? JobAdUrl { get; set; }
        // Add more properties as needed for future commands
    }
}