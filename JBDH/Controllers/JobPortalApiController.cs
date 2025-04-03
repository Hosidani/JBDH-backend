using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using JBDH.Models; // Assuming this is the namespace for your models

namespace JBDH.Controllers; // Assuming this is the namespace for your controller

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
 
var app = builder.Build();
app.UseCors();
app.UseSession();
app.MapControllers();
 
[ApiController]
[Route("api")]
public class JobPortalApiController : ControllerBase
{
    [HttpPost("submit_input")]
    public IActionResult SubmitInput([FromBody] Dictionary<string, string> data)
    {
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
 
                if (processedDescription.ToLower() == jobTitle.ToLower() && processedLocation == jobLocation)
                {
                    jobPriority = 1;
                }
                else if (processedDescription.ToLower() == jobTitle.ToLower() && processedLocation == "default")
                {
                    jobPriority = 2;
                }
                else if (processedLocation == jobLocation && processedDescription == "default")
                {
                    jobPriority = 3;
                }
                else if (processedLocation == jobLocation || processedDescription == "default")
                {
                    jobPriority = 4;
                }
                else if (processedDescription.ToLower() == jobTitle.ToLower() || processedLocation == "default")
                {
                    jobPriority = 5;
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
 
app.Run();