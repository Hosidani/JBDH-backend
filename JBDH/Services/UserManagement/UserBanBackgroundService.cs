using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using JBDH.Models;
using JBDH.Services;
using JBDH.Services.Configuration;
using JBDH.Models.Core;

namespace JBDH.Services.UserManagement
{
    public class UserBanBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromHours(24); // For testing


        public UserBanBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LoggingConfigService.ReturnInfo("🚀 UserBanBackgroundService started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                LoggingConfigService.ReturnInfo($"⏰ Checking for users to ban at {DateTime.UtcNow}");
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Find users who are not banned and have 10+ unique reports
                    var usersToBan = db.UserReports
                        .GroupBy(r => r.ReportedUserId)
                        .Select(g => new
                        {
                            ReportedUserId = g.Key,
                            UniqueReporters = g.Select(r => r.ReporterUserId).Distinct().Count()
                        })
                        // Below is when we ban a user if they have 2 or more unique reporters
                        .Where(x => x.UniqueReporters >= 2)
                        .Select(x => x.ReportedUserId)
                        .ToList();

                    var users = db.Users
                        .Where(u => usersToBan.Contains(u.Id) && !u.IsBanned)
                        .ToList();

                    foreach (var user in users)
                    {
                        user.IsBanned = true;
                    }

                    if (users.Count > 0)
                        await db.SaveChangesAsync();
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}