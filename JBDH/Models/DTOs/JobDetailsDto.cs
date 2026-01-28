using System.Text.Json;

namespace JBDH.Models.DTOs
{
    public class JobDetailsDto
    {
        public int JobId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Company { get; set; }
        public string? Location { get; set; }
        public DateTime? PostedDate { get; set; }
        public string? WebpageUrl { get; set; }
        
        // NYA FÄLT
        public string? WorkType { get; set; }
        public string? EmploymentType { get; set; }
        public string? ExternalJobId { get; set; }
        public DateTime? ApplicationDeadline { get; set; }
        public List<string> RequiredSkills { get; set; } = new();
        public List<string> RequiredSoftSkills { get; set; } = new();
        
        // USER ACTIONS
        public bool IsSaved { get; set; }
        public bool HasApplied { get; set; }
        public bool IsNotInterested { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// Convert Job model to JobDetailsDto for React API
        /// </summary>
        /// <param name="job">The job to convert</param>
        /// <param name="hasApplied">Whether user has applied to this job</param>
        /// <param name="isSaved">Whether user has saved this job</param>
        /// <param name="isNotInterested">Whether user marked as not interested</param>
        /// <param name="savedJobNotes">Notes from SavedJob if exists</param>
        /// <returns>JobDetailsDto</returns>
        public static JobDetailsDto FromJob(
            JBDH.Models.Job.Job job, 
            bool hasApplied = false, 
            bool isSaved = false, 
            bool isNotInterested = false,
            string? savedJobNotes = null)
        {
            var dto = new JobDetailsDto
            {
                JobId = job.JobId,
                Title = job.Title,
                Description = job.Description,
                Company = job.Company,
                Location = job.Location,
                PostedDate = job.PostedDate,
                WebpageUrl = job.WebpageUrl,
                WorkType = job.WorkType,
                EmploymentType = job.EmploymentType,
                ExternalJobId = job.ExternalJobId,
                ApplicationDeadline = job.ApplicationDeadline,
                IsSaved = isSaved,
                HasApplied = hasApplied,
                IsNotInterested = isNotInterested,
                Notes = savedJobNotes
            };

            // Parse RequiredSkills JSON to List<string>
            if (!string.IsNullOrEmpty(job.RequiredSkills))
            {
                try
                {
                    var skillsData = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(job.RequiredSkills);
                    if (skillsData != null)
                    {
                        if (skillsData.ContainsKey("hardSkills"))
                            dto.RequiredSkills = skillsData["hardSkills"];
                        
                        if (skillsData.ContainsKey("softSkills"))
                            dto.RequiredSoftSkills = skillsData["softSkills"];
                    }
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, treat as single skill
                    dto.RequiredSkills = new List<string> { job.RequiredSkills };
                }
            }

            return dto;
        }
    }
}