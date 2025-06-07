public class UserReport
{
    public int Id { get; set; }
    public int ReporterUserId { get; set; }
    public int ReportedUserId { get; set; }
    public int? DataFlowId { get; set; } // Nullable if not always set
    public string DataFlowText { get; set; } // Optional: store a snapshot of the text
    public string Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}