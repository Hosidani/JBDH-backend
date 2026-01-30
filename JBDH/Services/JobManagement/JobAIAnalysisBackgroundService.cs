using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using JBDH.Services.AICommunications;
using Microsoft.Extensions.DependencyInjection;

namespace JBDH.Services
{
    public class JobAIAnalysisBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobAIAnalysisBackgroundService> _logger;

        private volatile bool _shouldRun = false;
        private volatile bool _isPaused = false;
        private readonly object _lock = new();

        public JobAIAnalysisBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<JobAIAnalysisBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void StartProcessing()
        {
            lock (_lock)
            {
                _shouldRun = true;
                _isPaused = false;
                Monitor.PulseAll(_lock);
            }
            _logger.LogInformation("JobAIAnalysisBackgroundService: Start requested.");
        }

        public void PauseProcessing()
        {
            lock (_lock)
            {
                _isPaused = true;
            }
            _logger.LogInformation("JobAIAnalysisBackgroundService: Pause requested.");
        }

        public void StopProcessing()
        {
            lock (_lock)
            {
                _shouldRun = false;
                _isPaused = false;
                Monitor.PulseAll(_lock);
            }
            _logger.LogInformation("JobAIAnalysisBackgroundService: Stop requested.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var globalCts = scope.ServiceProvider.GetRequiredService<CancellationTokenSource>();
                    var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, globalCts.Token).Token;

                    var service = scope.ServiceProvider.GetRequiredService<IJobAIAnalysisService>();
                    await service.ProcessAllJobsWithAIAsync(10, 3000, linkedToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("⏹️ Job analysis cancelled gracefully - service stopping");
                    break;  // Graceful exit
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Job analysis failed - retrying in 5min");
                    await Task.Delay(300000, stoppingToken);  // 5min retry
                }
            }
        }
    }
}