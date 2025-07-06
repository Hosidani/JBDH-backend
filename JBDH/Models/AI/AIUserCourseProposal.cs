using System;
using JBDH.Models.User;
public class AIUserCourseProposal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ProposalJson { get; set; } // Store the AI JSON response
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; }
}
