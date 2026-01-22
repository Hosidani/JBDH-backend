using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
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
        _logger.LogInformation("JobSnapshot function started at: {time}", DateTime.UtcNow);
        await _jobService.DownloadFullJobSnapshotAsync("");
        _logger.LogInformation("JobSnapshot function completed at: {time}", DateTime.UtcNow);
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