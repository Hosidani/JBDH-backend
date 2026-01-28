using JBDH.Models.Core;
using JBDH.Models.DTOs;
using JBDH.Models.Job;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Services.JobManagement
{
    public interface ISavedJobService
    {
        Task SaveJobAsync(int userId, int jobId, string? notes = null);
        Task UnsaveJobAsync(int userId, int jobId);
        Task<bool> IsJobSavedAsync(int userId, int jobId);
        Task<List<JobDetailsDto>> GetSavedJobsAsync(int userId);
    }

    public class SavedJobService : ISavedJobService
    {
        private readonly ApplicationDbContext _context;

        public SavedJobService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SaveJobAsync(int userId, int jobId, string? notes = null)
        {
            // Check if job exists
            var jobExists = await _context.Jobs.AnyAsync(j => j.JobId == jobId);
            if (!jobExists)
                throw new ArgumentException("Job not found.", nameof(jobId));

            // Check if already saved
            var existingSavedJob = await _context.SavedJobs
                .FirstOrDefaultAsync(sj => sj.UserId == userId && sj.JobId == jobId);

            if (existingSavedJob != null)
                throw new InvalidOperationException("Job is already saved.");

            // Create new saved job
            var savedJob = new SavedJob
            {
                UserId = userId,
                JobId = jobId,
                Notes = notes,
                SavedAt = DateTime.UtcNow
            };

            _context.SavedJobs.Add(savedJob);
            await _context.SaveChangesAsync();
        }

        public async Task UnsaveJobAsync(int userId, int jobId)
        {
            var savedJob = await _context.SavedJobs
                .FirstOrDefaultAsync(sj => sj.UserId == userId && sj.JobId == jobId);

            if (savedJob == null)
                throw new ArgumentException("Saved job not found.");

            _context.SavedJobs.Remove(savedJob);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsJobSavedAsync(int userId, int jobId)
        {
            return await _context.SavedJobs
                .AnyAsync(sj => sj.UserId == userId && sj.JobId == jobId);
        }

        public async Task<List<JobDetailsDto>> GetSavedJobsAsync(int userId)
        {
            var savedJobsData = await _context.SavedJobs
                .Where(sj => sj.UserId == userId)
                .Include(sj => sj.Job)
                .OrderByDescending(sj => sj.SavedAt)
                .ToListAsync();

            var savedJobsDtos = new List<JobDetailsDto>();

            foreach (var savedJob in savedJobsData)
            {
                if (savedJob.Job != null)
                {
                    // Check if user has applied to this job
                    bool hasApplied = await _context.JobApplications
                        .AnyAsync(ja => ja.JobId == savedJob.JobId && ja.UserId == userId);

                    var dto = JobDetailsDto.FromJob(
                        job: savedJob.Job,
                        hasApplied: hasApplied,
                        isSaved: true,
                        isNotInterested: false,
                        savedJobNotes: savedJob.Notes
                    );

                    savedJobsDtos.Add(dto);
                }
            }

            return savedJobsDtos;
        }
    }
}