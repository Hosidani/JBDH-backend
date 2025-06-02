using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using JBDH.Services;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;

using JBDH.Services;

public class SearchCandidateService : ISearchCandidateService
{
    private readonly ApplicationDbContext _context;


    public SearchCandidateService(ApplicationDbContext context)
    {
        _context = context;

    }

    public List<SearchCandidateViewModel> SearchCandidates(string keyword, string location)
    {
        var users = _context.Users.Where(u => u.OpenForHiring == true);

        var userWithAI = users.Select(u => new
        {
            User = u,
            LatestAI = _context.UserAIResponses
                .Where(ai => ai.UserId == u.Id)
                .OrderByDescending(ai => ai.createdAt)
                .FirstOrDefault()
        });

        var filtered = userWithAI
            .Where(x =>
                x.LatestAI != null &&
                (string.IsNullOrEmpty(keyword) || 
                    x.LatestAI.responseText.ToLower().Contains(keyword.ToLower())) &&
                (string.IsNullOrEmpty(location) || 
                    (!string.IsNullOrEmpty(x.User.Location) && x.User.Location.ToLower() == location.ToLower()))
            )
            .ToList() // Materialize the query to avoid expression tree issues
            .Select(x => new SearchCandidateViewModel
            {
                UserId = x.User.Id,
                Username = x.User.Username,
                Location = x.User.Location,
                AIResponse = x.LatestAI != null ? x.LatestAI.responseText : null,
                AIResponseCreatedAt = x.LatestAI != null ? x.LatestAI.createdAt : DateTime.MinValue,
                Priority = (!string.IsNullOrEmpty(location) && !string.IsNullOrEmpty(x.User.Location) && x.User.Location.ToLower() == location.ToLower()) ? 1 : 2
            })
            .OrderBy(x => x.Priority)
            .ToList();
        if (filtered.Count == 0)
        {
            // If no candidates found, return an empty list
            LoggingConfigService.ReturnError("No candidates found for the given criteria.");
            return new List<SearchCandidateViewModel>();
        }
        LoggingConfigService.ReturnInfo($"Found {filtered.Count} candidates matching the criteria: keyword='{keyword}', location='{location}'");
        return filtered;
    }
}