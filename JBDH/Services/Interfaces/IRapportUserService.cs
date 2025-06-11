public interface IRapportUserService
{
    Task ReportUserAsync(int reporterUserId, int reportedUserId, string reason, int? dataFlowId = null, string dataFlowText = null);
}