using System.Linq;
using JBDH.Models; // Adjust namespace if needed
using Microsoft.EntityFrameworkCore;
using JBDH.Models.Core;
using JBDH.Models.Job;

namespace JBDH.Services.Core
{
    public class JobSyncService
    {
        private readonly ApplicationDbContext _dbContext;
        public JobSyncService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Syncs jobs from RawJobs to Jobs table. Adds new jobs and updates existing ones.
        /// </summary>
        public void SyncJobsFromRaw()
        {
            var rawJobs = _dbContext.RawJobs.AsNoTracking().ToList();
            // JBDH.Services.Configuration.LoggingConfigService.ReturnInfo($"[JobSyncService] Found {rawJobs.Count} raw jobs to sync.");
            int added = 0, updated = 0;
            foreach (var raw in rawJobs)
            {
                var job = _dbContext.Jobs.SingleOrDefault(j => j.JobId == raw.JobId);
                if (job == null)
                {
                    _dbContext.Jobs.Add(new Job
                    {
                        JobId = raw.JobId,
                        Title = raw.Title,
                        Description = raw.Description,
                        Company = raw.Company,
                        Location = raw.Location,
                        PostedDate = raw.PostedDate,
                        WebpageUrl = raw.WebpageUrl
                    });
                    added++;
                    //JBDH.Services.Configuration.LoggingConfigService.ReturnInfo($"[JobSyncService] Added job {raw.JobId}: {raw.Title}");
                }
                else
                {
                    job.Title = raw.Title;
                    job.Description = raw.Description;
                    job.Company = raw.Company;
                    job.Location = raw.Location;
                    job.PostedDate = raw.PostedDate;
                    job.WebpageUrl = raw.WebpageUrl;
                    updated++;
                    //JBDH.Services.Configuration.LoggingConfigService.ReturnInfo($"[JobSyncService] Updated job {raw.JobId}: {raw.Title}");
                }
            }
            _dbContext.SaveChanges();
            //JBDH.Services.Configuration.LoggingConfigService.ReturnInfo($"[JobSyncService] Sync complete. Added: {added}, Updated: {updated}");
        }
    }
}
