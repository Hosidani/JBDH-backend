using JBDH.Models.ViewModels;
using JBDH.Services.JobManagement;
using JBDH.Models.Core;
using Microsoft.AspNetCore.Mvc;
using JBDH.Swagger;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using JBDH.Models.Messaging;
using JBDH.Services.Friendship;
using JBDH.Services.Configuration;
using JBDH.Helpers;
using JBDH.Models.Job;
using JBDH.Models.DTOs;

namespace JBDH.Controllers
{
    [IncludeInApi]
    [Route("api/[controller]")]
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IJobService _jobService;
        private readonly IFriendService _friendService;
        public JobsController(ApplicationDbContext context, IJobService jobService, IFriendService friendService)
        {
            _context = context;
            _jobService = jobService;
            _friendService = friendService;
        }

        private int? GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out int userId) ? userId : null;
        }

        /// <summary>
        /// Get job details for React frontend
        /// </summary>
        /// <param name="id">Job ID</param>
        /// <returns>JobDetailsDto with all job information and user actions</returns>
        [Route("{id}")]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
                return NotFound("Job not found.");

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized("You must be logged in to view job details.");

            // Check user actions
            bool hasApplied = await _context.JobApplications.AnyAsync(a => a.JobId == id && a.UserId == userId.Value);
            
            var savedJob = await _context.SavedJobs
                .FirstOrDefaultAsync(sj => sj.UserId == userId.Value && sj.JobId == id);
            
            bool isSaved = savedJob != null;
            string? savedJobNotes = savedJob?.Notes;

            // Create DTO for React
            var dto = JobDetailsDto.FromJob(
                job: job,
                hasApplied: hasApplied,
                isSaved: isSaved,
                isNotInterested: false, // TODO: Implement NotInterested functionality
                savedJobNotes: savedJobNotes
            );

            return Ok(dto);
        }

        /// <summary>
        /// Send job to a friend via message
        /// </summary>
        /// <param name="jobId">Job ID to send</param>
        /// <param name="friendId">Friend's user ID</param>
        /// <returns>200 OK if successful</returns>
        [HttpPost]
        [Route("{jobId}/send-to-friend/{friendId}")]
        public async Task<IActionResult> SendToFriend(int jobId, int friendId)
        {
            var senderId = GetCurrentUserId();
            if (!senderId.HasValue)
                return Unauthorized("You must be logged in to send a job to a friend.");

            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null)
                return NotFound("Job not found.");

            // Check if friend exists
            var friendExists = await _context.Users.AnyAsync(u => u.Id == friendId);
            if (!friendExists)
                return NotFound("Friend not found.");

            var jobUrl = Url.Action("Details", "Jobs", new { id = jobId }, Request.Scheme);
            var messageText = $"Check out this job: <a href='{jobUrl}'>{job.Title}</a>";

            // Load sender and recipient for required properties
            var sender = await _context.Users.FindAsync(senderId.Value);
            var recipient = await _context.Users.FindAsync(friendId);

            if (sender == null || recipient == null)
                return BadRequest("Unable to load user information.");

            var message = new Message
            {
                SenderId = senderId.Value,
                Sender = sender,
                RecipientId = friendId,
                Recipient = recipient,
                Content = messageText,
                SentAt = DateTime.UtcNow
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Job sent to your friend's inbox!", jobId = jobId, friendId = friendId });
        }

        // SavedJob CRUD Endpoints

        /// <summary>
        /// Save a job for the current user
        /// </summary>
        /// <param name="jobId">The ID of the job to save</param>
        /// <returns>201 Created if successful</returns>
        [HttpPost("{jobId}/save")]
        public async Task<IActionResult> SaveJob(int jobId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized("You must be logged in to save jobs.");

            // Check if job exists
            var jobExists = await _context.Jobs.AnyAsync(j => j.JobId == jobId);
            if (!jobExists)
                return NotFound("Job not found.");

            // Check if already saved
            var existingSavedJob = await _context.SavedJobs
                .FirstOrDefaultAsync(sj => sj.UserId == userId.Value && sj.JobId == jobId);
            
            if (existingSavedJob != null)
                return Conflict("Job is already saved.");

            // Create new saved job
            var savedJob = new SavedJob
            {
                UserId = userId.Value,
                JobId = jobId,
                SavedAt = DateTime.UtcNow
            };

            _context.SavedJobs.Add(savedJob);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(IsSaved), 
                new { jobId = jobId }, 
                new { id = savedJob.Id, jobId = savedJob.JobId, savedAt = savedJob.SavedAt }
            );
        }

        /// <summary>
        /// Update notes for a saved job
        /// </summary>
        /// <param name="jobId">The ID of the job to update notes for</param>
        /// <param name="request">The update request containing notes</param>
        /// <returns>200 OK with updated saved job if successful</returns>
        [HttpPut("{jobId}/save")]
        public async Task<IActionResult> UpdateSavedJob(int jobId, [FromBody] UpdateSavedJobRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized("You must be logged in to update saved jobs.");

            var savedJob = await _context.SavedJobs
                .FirstOrDefaultAsync(sj => sj.UserId == userId.Value && sj.JobId == jobId);
            
            if (savedJob == null)
                return NotFound("Saved job not found.");

            // Update notes
            savedJob.Notes = request.Notes;
            
            _context.SavedJobs.Update(savedJob);
            await _context.SaveChangesAsync();

            return Ok(new { 
                id = savedJob.Id, 
                jobId = savedJob.JobId, 
                savedAt = savedJob.SavedAt,
                notes = savedJob.Notes 
            });
        }

        /// <summary>
        /// Remove a saved job for the current user
        /// </summary>
        /// <param name="jobId">The ID of the job to unsave</param>
        /// <returns>204 No Content if successful</returns>
        [HttpDelete("{jobId}/unsave")]
        public async Task<IActionResult> UnsaveJob(int jobId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized("You must be logged in to unsave jobs.");

            var savedJob = await _context.SavedJobs
                .FirstOrDefaultAsync(sj => sj.UserId == userId.Value && sj.JobId == jobId);
            
            if (savedJob == null)
                return NotFound("Saved job not found.");

            _context.SavedJobs.Remove(savedJob);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Check if a job is saved by the current user
        /// </summary>
        /// <param name="jobId">The ID of the job to check</param>
        /// <returns>Boolean indicating if the job is saved</returns>
        [HttpGet("{jobId}/issaved")]
        public async Task<IActionResult> IsSaved(int jobId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized("You must be logged in to check saved jobs.");

            var isSaved = await _context.SavedJobs
                .AnyAsync(sj => sj.UserId == userId.Value && sj.JobId == jobId);

            return Ok(new { jobId = jobId, isSaved = isSaved });
        }

        /// <summary>
        /// Get all saved jobs for the current user
        /// </summary>
        /// <returns>List of JobDetailsDto for saved jobs</returns>
        [HttpGet("saved")]
        public async Task<IActionResult> GetSavedJobs()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized("You must be logged in to view saved jobs.");

            var savedJobsData = await _context.SavedJobs
                .Where(sj => sj.UserId == userId.Value)
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
                        .AnyAsync(ja => ja.JobId == savedJob.JobId && ja.UserId == userId.Value);

                    var dto = JobDetailsDto.FromJob(
                        job: savedJob.Job,
                        hasApplied: hasApplied,
                        isSaved: true, // Obviously saved since we're in saved jobs
                        isNotInterested: false,
                        savedJobNotes: savedJob.Notes
                    );
                    
                    savedJobsDtos.Add(dto);
                }
            }

            return Ok(savedJobsDtos);
        }

        /// <summary>
        /// Mark a job as not interested for the current user
        /// </summary>
        /// <param name="jobId">The ID of the job to mark</param>
        /// <returns>200 OK if successful</returns>
        [HttpPost("{jobId}/not-interested")]
        public async Task<IActionResult> MarkNotInterested(int jobId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized("You must be logged in.");

            var existing = await _context.NotInterestedJobs
                .FirstOrDefaultAsync(nij => nij.UserId == userId.Value && nij.JobId == jobId);
            
            if (existing != null)
                return Conflict("Job already marked as not interested.");

            var notInterestedJob = new NotInterestedJob
            {
                UserId = userId.Value,
                JobId = jobId,
                MarkedAt = DateTime.UtcNow
            };

            _context.NotInterestedJobs.Add(notInterestedJob);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Job marked as not interested" });
        }

        /// <summary>
        /// Unmark a job as not interested for the current user
        /// </summary>
        /// <param name="jobId">The ID of the job to unmark</param>
        /// <returns>204 No Content if successful</returns>
        [HttpDelete("{jobId}/not-interested")]
        public async Task<IActionResult> UnmarkNotInterested(int jobId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized("You must be logged in.");

            var notInterestedJob = await _context.NotInterestedJobs
                .FirstOrDefaultAsync(nij => nij.UserId == userId.Value && nij.JobId == jobId);
            
            if (notInterestedJob == null)
                return NotFound("Job not marked as not interested.");

            _context.NotInterestedJobs.Remove(notInterestedJob);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class UpdateSavedJobRequest
    {
        public string? Notes { get; set; }
    }
}
