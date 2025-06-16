public class UserReport
{
    public int Id { get; set; }
    public int ReporterUserId { get; set; }
    public int ReportedUserId { get; set; }
    public int? DataFlowId { get; set; } // Nullable if not always set
    public string DataFlowText { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}