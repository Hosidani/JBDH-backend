using System.Collections.Generic;
using JBDH.Models.ViewModels;
using JBDH.Models.User;
using JBDH.Services.Helpers;
using Xunit;

namespace JBDH.Tests.Services.Helpers
{
    public class MatchingAlgorithmCVJobServiceTests
    {
        [Fact]
        public void CalculateJobTitleMatchPercentage_ReturnsCorrectPercentage()
        {
            var jobAI = new JobAdAIResponseViewModel
            {
                JobTitle = new List<string> { "Developer", "Engineer" }
            };
            var cvAI = new CVAnalysisResult
            {
                BestJobTitlesForUser = new List<string> { "Developer", "Manager" }
            };

            var service = new MatchingAlgorithmCVJobService();
            var percent = service.CalculateJobTitleMatchPercentage(jobAI, cvAI);

            Assert.Equal(50, percent); // 1 match out of 2
        }

        [Fact]
        public void CalculateJobTitleMatchPercentage_ReturnsZeroIfNoMatches()
        {
            var jobAI = new JobAdAIResponseViewModel { JobTitle = new List<string> { "Designer" } };
            var cvAI = new CVAnalysisResult { BestJobTitlesForUser = new List<string> { "Developer" } };

            var service = new MatchingAlgorithmCVJobService();
            var percent = service.CalculateJobTitleMatchPercentage(jobAI, cvAI);

            Assert.Equal(0, percent);
        }
    }
}