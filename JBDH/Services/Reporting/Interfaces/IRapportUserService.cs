namespace JBDH.Services.Reporting
{
    public interface IRapportUserService
    {
        Task<ReportResult> TryReportUserAsync(int reporterUserId, int reportedUserId, string reason, int? dataFlowId = null, string? dataFlowText = null);
    }
}