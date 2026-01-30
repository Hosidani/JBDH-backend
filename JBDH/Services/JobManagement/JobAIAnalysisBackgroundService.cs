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
            _logger.LogInformation("JobAIAnalysisBackgroundService started at: {time}", DateTimeOffset.Now);
            // Vänta 1 minut innan jobbet startar
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            _logger.LogInformation("JobAIAnalysisBackgroundService: Delay klar, startar huvudloop vid {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("JobAIAnalysisBackgroundService: Väntar på startkommando vid {time}", DateTimeOffset.Now);
                lock (_lock)
                {
                    while (!_shouldRun && !stoppingToken.IsCancellationRequested)
                        Monitor.Wait(_lock);
                    while (_isPaused && !stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("JobAIAnalysisBackgroundService: Paused at {time}", DateTimeOffset.Now);
                        Monitor.Wait(_lock);
                }
                _logger.LogInformation("JobAIAnalysisBackgroundService: Resumed at {time}", DateTimeOffset.Now);
                }
                if (stoppingToken.IsCancellationRequested || !_shouldRun)
                    break;

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var jobAIAnalysisService = scope.ServiceProvider.GetRequiredService<IJobAIAnalysisService>();
                        _logger.LogInformation("JobAIAnalysisBackgroundService: Processing started.");
                        await jobAIAnalysisService.ProcessAllJobsWithAIAsync(batchSize: 10, delayBetweenBatches: 3000);
                        _logger.LogInformation("JobAIAnalysisBackgroundService: Processing completed.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "JobAIAnalysisBackgroundService failed: {message}", ex.Message);
                }

                // Efter körning, vänta på ny start
                lock (_lock)
                {
                    _shouldRun = false;
                }
            }
        }
    }
}