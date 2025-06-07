using JBDH.Models; 
using Microsoft.EntityFrameworkCore;

public class RapportUserService : IRapportUserService
{
    private readonly ApplicationDbContext _context;
    public RapportUserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task ReportUserAsync(int reporterUserId, int reportedUserId, string reason, int? dataFlowId, string dataFlowText)
    {
        var reporterExists = await _context.Users.AnyAsync(u => u.Id == reporterUserId);
        var reportedExists = await _context.Users.AnyAsync(u => u.Id == reportedUserId);

        if (!reporterExists || !reportedExists)
            throw new ArgumentException("Reporter or reported user does not exist.");

        var report = new UserReport
        {
            ReporterUserId = reporterUserId,
            ReportedUserId = reportedUserId,
            Reason = reason,
            DataFlowId = dataFlowId,
            DataFlowText = dataFlowText,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserReports.Add(report);
        await _context.SaveChangesAsync();
    }

    public async Task ReportUserAsync(int reporterUserId, int reportedUserId, string reason)
    {
        await ReportUserAsync(reporterUserId, reportedUserId, reason, null, null);
    }
}