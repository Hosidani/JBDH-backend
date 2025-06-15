namespace JBDH.Models
{
    public class JobApplication
    {
        //UserId INT NOT NULL, JobId VARCHAR(255) NOT NULL, AppliedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, Notes TEXT, PRIMARY KEY (UserId, JobId), FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE, FOREIGN KEY (JobId) REFERENCES Jobs(JobId) ON DELETE CASCADE);"}
        public int ApplicationId { get; set;}
        public int UserId { get; set; }
        public int JobId { get; set; }
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
    }
}