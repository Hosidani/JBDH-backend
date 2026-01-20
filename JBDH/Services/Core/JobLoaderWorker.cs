using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using JBDH.Services.Configuration;

namespace JBDH.Services.Core
{
    public class JobLoaderWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public JobLoaderWorker(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            LoggingConfigService.ReturnInfo("JobLoaderWorker starting..."); 
            await Task.Delay(30000, token); // let app start first
            try
            {
                LoggingConfigService.ConfigLogging();
                LoggingConfigService.ReturnInfo("[STEP] Starting background job data fetch...");

                LoggingConfigService.ReturnInfo("[STEP] Creating JobStreamService instance...");
                var logger = _serviceProvider.GetRequiredService<ILogger<JobStreamService>>();
                var jobStreamService = new JobStreamService(_configuration, logger);

                LoggingConfigService.ReturnInfo("[STEP] Calling RunJobStreamAsync...");
                var jobData = await jobStreamService.RunJobStreamAsync(Array.Empty<string>());
                LoggingConfigService.ReturnInfo($"[STEP] RunJobStreamAsync completed. Jobs fetched: {jobData?.Count ?? -1}");

                LoggingConfigService.ReturnInfo("[STEP] Creating DI scope for DatabaseHandlerService...");
                using (var scope = _serviceProvider.CreateScope())
                {
                    LoggingConfigService.ReturnInfo("[STEP] Resolving DatabaseHandlerService...");
                    var dbHandler = scope.ServiceProvider.GetRequiredService<JBDH.Services.Core.DatabaseHandlerService>();
                    LoggingConfigService.ReturnInfo("[STEP] Calling DatabaseHandler with job data...");
                    if (jobData != null)
                    {
                        await dbHandler.DatabaseHandlerAsync(jobData);
                        LoggingConfigService.ReturnInfo("[STEP] DatabaseHandler completed.");
                    }
                    else
                    {
                        LoggingConfigService.ReturnError("[ERROR] jobData is null. Skipping DatabaseHandler call.");
                    }
                }
                LoggingConfigService.ReturnInfo("[STEP] Background job data fetch completed.");
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"[ERROR] Background job fetch error: {ex.Message}");
                // Don't rethrow - let the app continue even if job fetching fails
            }
        }
    }
}
