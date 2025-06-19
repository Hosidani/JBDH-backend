using System.Threading.Tasks;
using JBDH.Models.ViewModels;
using JBDH.Models;
using Microsoft.EntityFrameworkCore;
using JBDH.Services.JobManagement;

namespace JBDH.Services.JobManagement
{
    public class JobService : IJobService
    {
        private readonly ApplicationDbContext _context;

        public JobService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<JobDetailsViewModel?> GetJobDetailsAsync(int jobId, string? username)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return null;

            var user = username != null ? await _context.Users.FirstOrDefaultAsync(u => u.Username == username) : null;
            var hasApplied = user != null && await _context.JobApplications.AnyAsync(j => j.UserId == user.Id && j.JobId == jobId);

            return new JobDetailsViewModel
            {
                Job = job,
                HasApplied = hasApplied
            };
        }
    }
}