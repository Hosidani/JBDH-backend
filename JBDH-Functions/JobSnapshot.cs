using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using JBDH.Services.Core;

namespace JBDH.Function;

public class JobSnapshot
{
    private readonly ILogger _logger;
    private readonly JobStreamService _jobService;

    public JobSnapshot(ILoggerFactory loggerFactory, JobStreamService jobService)
    {
        _logger = loggerFactory.CreateLogger<JobSnapshot>();
        _jobService = jobService;
    }

    [Function("JobSnapshot")]
    public async Task Run([TimerTrigger("0 2 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("JobSnapshot function started at: {time}", DateTime.Now);
        await _jobService.RunJobStreamAsync(Array.Empty<string>());
        _logger.LogInformation("JobSnapshot function completed at: {time}", DateTime.Now);
    }
}