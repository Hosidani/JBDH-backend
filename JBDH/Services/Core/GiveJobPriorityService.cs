using JBDH.Models;
using JBDH.Models.Job;
using System.Collections.Generic;
using System.Linq;

namespace JBDH.Services
{
    public class GiveJobPriorityService
    {
        public static List<JobSearchResultViewModel> AssignJobPriority(
            List<Job> jobs,
            string description,
            string location)
        {
            LoggingConfigService.ReturnInfo("GiveJobPriority service called");
            var presentJobs = new List<JobSearchResultViewModel>();

            foreach (var job in jobs)
            {
                int jobPriority = 0;

                // Skip this job if title or location is invalid
                if (string.IsNullOrEmpty(job.Title) || string.IsNullOrEmpty(job.Location))
                    continue;

                bool hasDescription = !string.IsNullOrEmpty(description);
                bool hasLocation = !string.IsNullOrEmpty(location);

                if (hasDescription && hasLocation)
                {
                    if (job.Title.Contains(description, System.StringComparison.OrdinalIgnoreCase) &&
                        job.Location.Contains(location, System.StringComparison.OrdinalIgnoreCase))
                    {
                        jobPriority = 1;
                    }
                    else if (job.Title.Contains(description, System.StringComparison.OrdinalIgnoreCase))
                    {
                        jobPriority = 2;
                    }
                    else if (job.Location.Contains(location, System.StringComparison.OrdinalIgnoreCase))
                    {
                        jobPriority = 3;
                    }
                    else
                    {
                        jobPriority = 4; // Least relevant
                    }
                }
                else if (hasDescription)
                {
                    if (job.Title.Contains(description, System.StringComparison.OrdinalIgnoreCase))
                        jobPriority = 1;
                    else
                        jobPriority = 2;
                }
                else if (hasLocation)
                {
                    if (job.Location.Contains(location, System.StringComparison.OrdinalIgnoreCase))
                        jobPriority = 1;
                    else
                        jobPriority = 2;
                }
                else
                {
                    jobPriority = 4;
                }

                presentJobs.Add(new JobSearchResultViewModel
                {
                    Job = job,
                    Priority = jobPriority
                });
            }

            LoggingConfigService.ReturnInfo($"Presenting {presentJobs.Count} jobs.");
            return presentJobs.OrderBy(j => j.Priority).ToList();
        }
    }
}