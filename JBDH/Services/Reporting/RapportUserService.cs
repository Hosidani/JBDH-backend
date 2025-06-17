using JBDH.Models;
using JBDH.Models.User;
using Microsoft.EntityFrameworkCore;

public class ReportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RapportUserService : IRapportUserService
{
    private readonly ApplicationDbContext _context;
    public RapportUserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReportResult> TryReportUserAsync(int reporterUserId, int reportedUserId, string reason, int? dataFlowId, string? dataFlowText)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return new ReportResult { Success = false, ErrorMessage = "Reason cannot be empty." };

        var reporterExists = await _context.Users.AnyAsync(u => u.Id == reporterUserId);
        var reportedExists = await _context.Users.AnyAsync(u => u.Id == reportedUserId);

        if (!reporterExists || !reportedExists)
            return new ReportResult { Success = false, ErrorMessage = "Reporter or reported user does not exist." };

        var report = new UserReport
        {
            ReporterUserId = reporterUserId,
            ReportedUserId = reportedUserId,
            Reason = reason,
            DataFlowId = dataFlowId,
            DataFlowText = dataFlowText ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserReports.Add(report);
        await _context.SaveChangesAsync();

        return new ReportResult { Success = true };
    }
}