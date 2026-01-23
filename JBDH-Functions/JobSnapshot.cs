using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using JBDH.Services.Core;
using JBDH.Services.Core;

public class JobSnapshot
{
    private readonly ILogger<JobSnapshot> _logger;
    private readonly JobStreamService _jobService;

    public JobSnapshot(ILogger<JobSnapshot> logger, JobStreamService jobService)
    {
        _logger = logger;
        _jobService = jobService;
    }

    [Function("JobSnapshot")]
    public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🔥 STEG 1: Function startad! ({time})", startTime);

        _logger.LogInformation("🔥 STEG 2: Kallar Snapshot API...");
        var apiStart = DateTime.UtcNow;
        await _jobService.DownloadFullJobSnapshotAsync("");
        var apiEnd = DateTime.UtcNow;
        var apiDuration = apiEnd - apiStart;
        _logger.LogInformation("🔥 STEG 3: API svarade efter {duration}", apiDuration.ToString(@"hh\:mm\:ss"));

        _logger.LogInformation("🔥 STEG 4: Upload till Blob...");
        // Om du har separat kod för upload, logga tiden här

        var totalDuration = apiEnd - startTime;
        _logger.LogInformation("✅ TOTALTID: {duration} för 460MB!", totalDuration.ToString(@"hh\:mm\:ss"));
    }

    // HTTP-triggerad function för manuell körning.
    [Function("JobSnapshotHttp")] // Namn på functionen i Azure
    public async Task<HttpResponseData> RunHttp(
        [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequestData req)
    {
        _logger.LogInformation("JobSnapshot HTTP-trigger started at: {time}", DateTime.UtcNow);
        await _jobService.DownloadFullJobSnapshotAsync("");
        _logger.LogInformation("JobSnapshot HTTP-trigger completed at: {time}", DateTime.UtcNow);
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync("JobSnapshot HTTP-trigger completed.");
        return response;
    }
}