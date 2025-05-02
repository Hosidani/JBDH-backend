namespace JBDH.Services
{
    public class GiveJobPriorityService
    {
        public static List<(int Priority, Dictionary<string, object> Job)> AssignJobPriority(
    List<Dictionary<string, object>> results,
    string description,
    string location)
        {
            LoggingConfigService.ReturnInfo("GiveJobPriority service called");
            var presentJobs = new List<(int Priority, Dictionary<string, object> Job)>();
            foreach (var job in results)
            {
                var jobTitle = job.GetValueOrDefault("title", "").ToString();
                var jobLocation = job.GetValueOrDefault("location", "").ToString();
                int jobPriority = 0;

                // Skip this job if title or location is invalid
                if (string.IsNullOrEmpty(jobTitle) || string.IsNullOrEmpty(jobLocation))
                {
                    continue;
                }

                if (jobTitle.Contains(description, StringComparison.OrdinalIgnoreCase) &&
                    jobLocation.Contains(location, StringComparison.OrdinalIgnoreCase))
                {
                    jobPriority = 1;
                }
                else if (jobTitle.Contains(description, StringComparison.OrdinalIgnoreCase))
                {
                    jobPriority = 2;
                }

                if (jobPriority > 0)
                {
                    presentJobs.Add((jobPriority, job));
                }
            }
            LoggingConfigService.ReturnInfo($"Presenting {presentJobs.Count} jobs.");
            return presentJobs.OrderBy(j => j.Priority).ToList();
        }

    }
}