using JBDH.Models.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using JBDH.Services.AICommunications;
using JBDH.Models;
using Microsoft.EntityFrameworkCore;
using JBDH.Models.AI;
using JBDH.Services; // <-- Lägg till denna
using Microsoft.Extensions.Hosting;

namespace JBDH.Controllers
{
    // Development-only controller to dump ApiDescription info for debugging Swagger/document generation.
    [ApiController]
    [Route("api/dev")]
    public class DevController : ControllerBase
    {
        private readonly IApiDescriptionGroupCollectionProvider _provider;
        private readonly ILogger<DevController> _logger;
        private readonly IJobAIAnalysisService _jobAIAnalysisService;
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        public DevController(
            IApiDescriptionGroupCollectionProvider provider,
            ILogger<DevController> logger,
            IJobAIAnalysisService jobAIAnalysisService,
            ApplicationDbContext context,
            IServiceProvider serviceProvider) 
        {
            _provider = provider;
            _logger = logger;
            _jobAIAnalysisService = jobAIAnalysisService;
            _context = context;
            _serviceProvider = serviceProvider;
        }

        [HttpGet("apidescriptions")]
        public IActionResult GetApiDescriptions()
        {
            var groups = _provider.ApiDescriptionGroups.Items;
            var list = groups.SelectMany(g => g.Items).Select(d => new
            {
                RelativePath = d.RelativePath,
                HttpMethod = d.HttpMethod,
                ActionDisplayName = d.ActionDescriptor?.DisplayName,
                EndpointMetadata = d.ActionDescriptor?.EndpointMetadata?.Select(m => m.GetType().FullName).ToList(),
                SupportedResponseMediaTypes = d.SupportedResponseTypes.SelectMany(rt => rt.ApiResponseFormats.Select(f => f.MediaType)).ToList(),
                HasIncludeInApiAttribute = HasIncludeInApi(d)
            }).ToList();

            return Ok(list);
        }

        private bool HasIncludeInApi(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription d)
        {
            // Check endpoint metadata
            if (d.ActionDescriptor?.EndpointMetadata?.OfType<JBDH.Swagger.IncludeInApiAttribute>().Any() == true)
                return true;

            // Try ControllerActionDescriptor reflection
            if (d.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor cad)
            {
                if (cad.MethodInfo.GetCustomAttributes(typeof(JBDH.Swagger.IncludeInApiAttribute), true).Any())
                    return true;
                if (cad.ControllerTypeInfo.GetCustomAttributes(typeof(JBDH.Swagger.IncludeInApiAttribute), true).Any())
                    return true;
            }

            return false;
        }

        [HttpPost("bulk-process-jobs")]
        public async Task<IActionResult> BulkProcessJobs([FromBody] BulkProcessRequest request)
        {
            try
            {
                _logger.LogInformation($"Starting bulk processing with batch size: {request.BatchSize}");
                
                await _jobAIAnalysisService.ProcessAllJobsWithAIAsync(
                    batchSize: request.BatchSize, 
                    delayBetweenBatches: request.DelayBetweenBatches);
                
                return Ok(new { message = "Bulk processing completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Bulk processing failed: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("bulk-process-status")]
        public async Task<IActionResult> GetBulkProcessStatus()
        {
            try
            {
                var totalJobs = await _context.Jobs.CountAsync();
                
                var processedJobs = await _context.JobAIResponses
                    .Where(r => r.Status == JobStatus.Completed)
                    .CountAsync();
                    
                var failedJobs = await _context.JobAIResponses
                    .Where(r => r.Status == JobStatus.Failed)
                    .CountAsync();
                    
                var processingJobs = await _context.JobAIResponses
                    .Where(r => r.Status == JobStatus.Processing)
                    .CountAsync();
                
                var pendingJobs = totalJobs - processedJobs - failedJobs - processingJobs;
                
                return Ok(new
                {
                    TotalJobs = totalJobs,
                    ProcessedJobs = processedJobs,
                    FailedJobs = failedJobs, 
                    ProcessingJobs = processingJobs,
                    PendingJobs = pendingJobs,
                    CompletionPercentage = totalJobs > 0 ? (processedJobs * 100.0) / totalJobs : 0,
                    StatusBreakdown = new
                    {
                        Completed = processedJobs,
                        Failed = failedJobs,
                        Processing = processingJobs,
                        Pending = pendingJobs
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("start-bulk-processing")]
        public IActionResult StartBulkProcessing()
        {
            var backgroundService = _serviceProvider
                .GetServices<IHostedService>()
                .OfType<JobAIAnalysisBackgroundService>()
                .FirstOrDefault();
            backgroundService?.StartProcessing();
            return Ok();
        }

        [HttpPost("pause-bulk-processing")]
        public IActionResult PauseBulkProcessing()
        {
            var backgroundService = _serviceProvider
                .GetServices<IHostedService>()
                .OfType<JobAIAnalysisBackgroundService>()
                .FirstOrDefault();
            backgroundService?.PauseProcessing();
            return Ok();
        }

        [HttpPost("stop-bulk-processing")]
        public IActionResult StopBulkProcessing()
        {
            var backgroundService = _serviceProvider
                .GetServices<IHostedService>()
                .OfType<JobAIAnalysisBackgroundService>()
                .FirstOrDefault();
            backgroundService?.StopProcessing();
            return Ok();
        }

        public class BulkProcessRequest
        {
            public int BatchSize { get; set; } = 5; // Mindre batches för test
            public int DelayBetweenBatches { get; set; } = 3000; // 3 sekunder
        }
    }
}
