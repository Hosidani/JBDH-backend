using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using JBDH.Models;
using System.Globalization;

namespace JBDH.Controllers
{
    [ApiController]
    [Route("api")]
    public class JobPortalApiController : ControllerBase
    {
        [HttpPost("submit_input")]
        public IActionResult SubmitInput([FromBody] Dictionary<string, string> data)
        {
            Console.WriteLine("SubmitInput method called");
            try
            {
                if (!data.ContainsKey("description") || !data.ContainsKey("location") || !data.ContainsKey("date"))
                {
                    return BadRequest(new { success = false, error = "Missing required fields" });
                }

                HttpContext.Session.SetString("description", data["description"]);
                HttpContext.Session.SetString("location", data["location"]);
                HttpContext.Session.SetString("date", data["date"]);

                return Ok(new { success = true, message = "Input received successfully" });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Input submission failed: {e.Message}");
                return StatusCode(500, new { success = false, error = "Internal server error" });
            }
        }

        [HttpGet("jobs")]
        public IActionResult GetJobs([FromQuery] string description, [FromQuery] string location, [FromQuery] string date)
        {
            Console.WriteLine("GetJobs method called");
            try
            {
                var (processedDescription, processedLocation, processedDate) = InternalSorting.GetInputFromUser(description, location, date);
                Console.WriteLine($"Processed inputs - Description: {processedDescription}, Location: {processedLocation}, Date: {processedDate}");

                var results = InternalSorting.InternalSortingFunction(processedDescription, processedLocation);
                Console.WriteLine($"Returning {results.Count} job results");

                var presentJobs = new List<(int Priority, Dictionary<string, object> Job)>();
                foreach (var job in results)
                {
                    var jobTitle = job.GetValueOrDefault("title", "").ToString();
                    var jobLocation = job.GetValueOrDefault("location", "").ToString();
                    int? jobPriority = null;

                    // Skip this job if title or location is invalid
                    /*if (string.IsNullOrEmpty(jobTitle) || string.IsNullOrEmpty(jobLocation))
                    {
                        continue;
                    }*/

                    Console.WriteLine($"Processing Job: Title = {jobTitle}, Location = {jobLocation}");
                    Console.WriteLine($"Processed Description: {processedDescription}, Processed Location: {processedLocation}");

                    // Adjust priority based on conditions
                    if (jobTitle.Contains(processedDescription, StringComparison.OrdinalIgnoreCase) &&
                        jobLocation.Contains(processedLocation, StringComparison.OrdinalIgnoreCase))
                    {
                        jobPriority = 1;
                        //Console.WriteLine($"Assigned Priority 1: Title = {jobTitle}, Location = {jobLocation}");
                    }
                    else if (jobTitle.Contains(processedDescription, StringComparison.OrdinalIgnoreCase) && processedLocation == "default")
                    {
                        jobPriority = 2;
                        //Console.WriteLine($"Assigned Priority 2: Title = {jobTitle}, Location = {jobLocation}");
                    }
                    else if (processedLocation == jobLocation)
                    {
                        jobPriority = 3;
                        Console.WriteLine($"Assigned Priority 3: Title = {jobTitle}, Location = {jobLocation}");
                    }
                    else if (jobTitle.Contains(processedDescription, StringComparison.OrdinalIgnoreCase) && processedLocation == jobLocation)
                    {
                        jobPriority = 4;
                        Console.WriteLine($"Assigned Priority 4: Title = {jobTitle}, Location = {jobLocation}");
                    }
                    else
                    {
                        jobPriority = 5;
                        Console.WriteLine($"Assigned Priority 5: Title = {jobTitle}, Location = {jobLocation}");
                    }

                    if (jobPriority.HasValue)
                    {
                        presentJobs.Add((jobPriority.Value, job));
                    }
                }

                presentJobs.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                var sortedJobs = presentJobs.Select(x => x.Job).ToList();

                return Ok(new
                {
                    success = true,
                    count = sortedJobs.Count,
                    jobs = sortedJobs
                });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Job fetch failed: {e.Message}");
                return StatusCode(500, new { success = false, error = "Internal server error" });
            }
        }
    }
}
