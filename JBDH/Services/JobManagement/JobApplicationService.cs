using System;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.Services;
using JBDH.Models.Job;
using JBDH.Services.Configuration;
using Microsoft.EntityFrameworkCore;


namespace JBDH.Services.JobManagement
{
    public class JobApplicationService : IJobApplicationService
    {
        private readonly ApplicationDbContext _context;

        public JobApplicationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasUserAppliedAsync(int userId, int jobId)
        {
            LoggingConfigService.ReturnInfo($"Checking if user {userId} has applied for job {jobId}.");
            return await _context.JobApplications
                .AnyAsync(ja => ja.UserId == userId && ja.JobId == jobId);
        }

        public async Task<ServiceResult> ApplyJobAsync( int userId, int jobId, string notes)
        {
            LoggingConfigService.ReturnInfo($"User {userId} is applying for job {jobId} with notes: {notes}.");
            if (await HasUserAppliedAsync(userId, jobId))
                return new ServiceResult { Success = false, ErrorMessage = "User has already applied for this job." };

            var application = new JobApplication
            {
                UserId = userId,
                JobId = jobId,
                AppliedAt = DateTime.UtcNow,
                Notes = notes
            };
            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();
            return new ServiceResult { Success = true };
        }

        public async Task<ServiceResult> UnapplyJobAsync(int userId, int jobId)
        {
            LoggingConfigService.ReturnInfo($"User {userId} is unapplying from job {jobId}.");
            var app = await _context.JobApplications
                .FirstOrDefaultAsync(ja => ja.UserId == userId && ja.JobId == jobId);
            if (app == null)
                return new ServiceResult { Success = false, ErrorMessage = "No application found to remove." };
            _context.JobApplications.Remove(app);
            await _context.SaveChangesAsync();
            return new ServiceResult { Success = true };
        }
    }
}