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

        public JobLoaderWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            LoggingConfigService.ReturnInfo("JobLoaderWorker starting..."); 
            await Task.Delay(30000, token); // let app start first
            try
            {
                LoggingConfigService.ConfigLogging();
                LoggingConfigService.ReturnInfo("Starting background job data fetch...");
                var jobData = await JobStreamService.RunJobStreamAsync(Array.Empty<string>());
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbHandler = scope.ServiceProvider.GetRequiredService<JBDH.Services.Core.DatabaseHandlerService>();
                    dbHandler.DatabaseHandler(jobData);
                }
                LoggingConfigService.ReturnInfo("Background job data fetch completed.");
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Background job fetch error: {ex.Message}");
                // Don't rethrow - let the app continue even if job fetching fails
            }
        }
    }
}
