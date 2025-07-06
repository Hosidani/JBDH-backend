using System;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.User;
using JBDH.Models.AI;
using JBDH.Services.AICommunications;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using JBDH.Services.Configuration;

namespace JBDH.Services.AICommunications
{
    public class AIUserCourseProposalService : IAIUserCourseProposalService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IAIApiClient _aiApiClient;

        public AIUserCourseProposalService(ApplicationDbContext dbContext, IAIApiClient aiApiClient)
        {
            _dbContext = dbContext;
            _aiApiClient = aiApiClient;
        }
        public async Task GenerateAndSaveCourseProposalAsync(User user)
        {
            // Make sure the user has a CV path
            if (string.IsNullOrWhiteSpace(user.CVPath))
            {
                LoggingConfigService.ReturnError("User CVPath is null or empty, cannot generate course proposal.");
                return;
            }

            var aiRequest = new AIRequest
            {
                CommandId = 4,
                CVPath = user.CVPath // Pass the path to the CV PDF
            };

            var proposalJson = await _aiApiClient.GetAIResponse(aiRequest);

            if (string.IsNullOrWhiteSpace(proposalJson) || proposalJson.Trim() == "User is null")
            {
                LoggingConfigService.ReturnError("AI did not return a valid course proposal.");
                return;
            }

            var proposal = new AIUserCourseProposal
            {
                UserId = user.Id,
                ProposalJson = proposalJson
            };

            _dbContext.AIUserCourseProposals.Add(proposal);
            await _dbContext.SaveChangesAsync();
        }
    }
}