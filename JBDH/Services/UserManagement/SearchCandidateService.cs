using JBDH.Models.Core;
using JBDH.Models.ViewModels;
using JBDH.Services.AICommunications;
using JBDH.Services.Configuration;
using Microsoft.EntityFrameworkCore;
using JBDH.Models.AI;


namespace JBDH.Services.UserManagement
{
    public class SearchCandidateService : ISearchCandidateService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserAIResponseService _userAIResponseService;

    public SearchCandidateService(ApplicationDbContext context, IUserAIResponseService userAIResponseService)
    {
        _context = context;
        _userAIResponseService= userAIResponseService;
    }

        public async Task<List<SearchCandidateViewModel>> SearchCandidates(string keyword, string location)
        {
            var users = _context.Users.Where(u => u.OpenForHiring == true);
            LoggingConfigService.ReturnInfo($"Total users open for hiring: {await users.CountAsync()}");

            // Normalize input parameters
            keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword?.Trim();
            location = string.IsNullOrWhiteSpace(location) ? null : location?.Trim();

            var userWithAI = users.Select(u => new
            {
                User = u,
                LatestAI = (UserAIResponse?)_context.UserAIResponses
                    .Where(ai => ai.UserId == u.Id)
                    .OrderByDescending(ai => ai.CreatedAt)
                    .FirstOrDefault()
            });

            var filtered = await userWithAI
                .Where(x =>
                    x.LatestAI != null &&
                    (keyword == null ||
                        (!string.IsNullOrEmpty(x.LatestAI!.ResponseText) && x.LatestAI.ResponseText.ToLower().Contains(keyword.ToLower()))) &&
                    (location == null ||
                        (!string.IsNullOrEmpty(x.User!.Location) && x.User.Location.ToLower().Contains(location.ToLower())))
                )
                .ToListAsync();

            LoggingConfigService.ReturnInfo($"Filtered users count: {filtered.Count}");

            // Bygg ViewModels och prioritera
            var candidates = await Task.WhenAll(filtered.Select(async x =>
            {
                var aiResponse = await _userAIResponseService.GetLatestUserAIResponseAsync(x.User.Id);
                LoggingConfigService.ReturnInfo($"UserId: {x.User.Id}, Email: {x.User.Email}, LatestAIResponse: {(aiResponse != null ? aiResponse.ResponseText : "NULL")}");
                return new SearchCandidateViewModel
                {
                    UserId = x.User.Id,
                    Email = x.User.Email, // Use Email instead of Username
                    Location = x.User.Location,
                    UserAIResponse = aiResponse ?? new UserAIResponse { UserId = x.User.Id, User = x.User },
                    Priority = (location != null && !string.IsNullOrEmpty(x.User.Location) && x.User.Location.ToLower().Contains(location.ToLower())) ? 1 : 2
                };
            }));

            var prioritized = candidates.OrderBy(x => x.Priority).ToList();

            if (prioritized.Count == 0)
            {
                LoggingConfigService.ReturnError("No candidates found for the given criteria.");
                return new List<SearchCandidateViewModel>();
            }
            LoggingConfigService.ReturnInfo($"Found {prioritized.Count} candidates matching the criteria: keyword='{keyword}', location='{location}'");
            return prioritized;
        }
    }
}