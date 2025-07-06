using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using JBDH.Controllers;
using JBDH.Models.AI;
using JBDH.Models.Core;
using JBDH.Models.User;
using JBDH.Models.ViewModels;
using JBDH.Services.AICommunications;
using JBDH.Services.Helpers;
using JBDH.Services.JobManagement;
using JBDH.Services.UserManagement;
using JBDH.Services.Workflows;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace JBDH.Tests.Controllers
{
    public class SearchControllerTests
    {
        private SearchController CreateController(
            Mock<IUserPropertiesService> userPropertiesService = null,
            Mock<IAIRequestBuilderService> aiRequestBuilderService = null,
            Mock<ISearchCandidateService> searchCandidateService = null,
            Mock<IAIApiClient> aiService = null,
            Mock<ISearchJobsService> searchJobsService = null,
            Mock<IUserAIResponseService> userAIResponseService = null,
            Mock<IJobAIResponseService> jobAIResponseService = null,
            Mock<IJobAIAnalysisService> jobAIAnalysisService = null,
            Mock<ISearchWorkflowService> searchWorkflowService = null,
            ApplicationDbContext context = null, // <-- change type here
            Mock<IManageIOService> manageIOService = null,
            Mock<IMatchingAlgorithmCVJobService> matchingAlgorithmCVJobService = null
        )
        {
            // If no context provided, create a new in-memory one
            if (context == null)
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;
                context = new ApplicationDbContext(options);
            }

            return new SearchController(
                userPropertiesService?.Object ?? new Mock<IUserPropertiesService>().Object,
                aiRequestBuilderService?.Object ?? new Mock<IAIRequestBuilderService>().Object,
                searchCandidateService?.Object ?? new Mock<ISearchCandidateService>().Object,
                aiService?.Object ?? new Mock<IAIApiClient>().Object,
                searchJobsService?.Object ?? new Mock<ISearchJobsService>().Object,
                userAIResponseService?.Object ?? new Mock<IUserAIResponseService>().Object,
                jobAIResponseService?.Object ?? new Mock<IJobAIResponseService>().Object,
                jobAIAnalysisService?.Object ?? new Mock<IJobAIAnalysisService>().Object,
                searchWorkflowService?.Object ?? new Mock<ISearchWorkflowService>().Object,
                context, // <-- pass the real context
                manageIOService?.Object ?? new Mock<IManageIOService>().Object,
                matchingAlgorithmCVJobService?.Object ?? new Mock<IMatchingAlgorithmCVJobService>().Object
            );
        }

        private void SetUserContext(Controller controller, string userId = "1")
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "mock"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task SearchForJobs_ReturnsViewWithResults()
        {
            var mockWorkflow = new Mock<ISearchWorkflowService>();
            mockWorkflow.Setup(x => x.SearchAndAttachMetaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<JobSearchResultViewModel> { new JobSearchResultViewModel() });

            var controller = CreateController(searchWorkflowService: mockWorkflow);
            SetUserContext(controller);

            var result = await controller.SearchForJobs("desc", "loc");

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("SearchResultsJobs", viewResult.ViewName);
            Assert.NotNull(viewResult.Model);
        }

        [Fact]
        public void SearchJob_ReturnsView()
        {
            var controller = CreateController();
            var result = controller.SearchJob();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Search_ReturnsView()
        {
            var controller = CreateController();
            var result = controller.Search();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task AnalyzeJob_ReturnsPartialView_WhenValid()
        {

            // Arrange
            var mockUserPropertiesService = new Mock<IUserPropertiesService>();
            mockUserPropertiesService.Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(new User
                {
                    Id = 1,
                    Username = "testuser",
                    PasswordHash = "hash",
                    Email = "test@example.com",
                    Location = "Testville"
                });

            var mockUserAIResponseService = new Mock<IUserAIResponseService>();
            mockUserAIResponseService.Setup(x => x.GetLatestUserAIResponseAsync(It.IsAny<int>()))
                .ReturnsAsync(new UserAIResponse
                {
                    Id = 1,
                    UserId = 1,
                    ResponseText = "{\"JobTitle\": [\"Developer\"]}",
                    CreatedAt = DateTime.UtcNow, // <-- Add this line
                    User = new User
                    {
                        Id = 1,
                        Username = "testuser",
                        PasswordHash = "hash",
                        Email = "test@example.com",
                        Location = "Testville"
                    }
                });

            var mockJobAIAnalysisService = new Mock<IJobAIAnalysisService>();
            mockJobAIAnalysisService.Setup(x => x.GetOrCreateAnalysisAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync("{ \"JobTitle\": [\"Developer\"] }"); // This must match your JobAdAIResponseViewModel

            var mockManageIOService = new Mock<IManageIOService>();
            mockManageIOService.Setup(x => x.ExtractInnerJsonFromJobAIResponse(It.IsAny<string>()))
                .Returns("{ \"JobTitle\": [\"Developer\"] }"); // This must match your JobAdAIResponseViewModel

            var mockMatchingAlgorithmCVJobService = new Mock<IMatchingAlgorithmCVJobService>();
            mockMatchingAlgorithmCVJobService
                .Setup(x => x.CalculateJobTitleMatchPercentage(It.IsAny<JobAdAIResponseViewModel>(), It.IsAny<CVAnalysisResult>()))
                .Returns(100);

            // Use a real in-memory ApplicationDbContext
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options);

            // Create the controller with all dependencies
            var controller = new SearchController(
                mockUserPropertiesService.Object,
                new Mock<IAIRequestBuilderService>().Object,
                new Mock<ISearchCandidateService>().Object,
                new Mock<IAIApiClient>().Object,
                new Mock<ISearchJobsService>().Object,
                mockUserAIResponseService.Object,
                new Mock<IJobAIResponseService>().Object,
                mockJobAIAnalysisService.Object,
                new Mock<ISearchWorkflowService>().Object,
                context,
                mockManageIOService.Object,
                mockMatchingAlgorithmCVJobService.Object
            );

            // Act
            var result = await controller.AnalyzeJob("url", "1");

            // Assert
            var partial = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_JobAdAIResultPartial", partial.ViewName);
        }

        [Fact]
        public async Task SearchCandidate_ReturnsViewWithResults()
        {
            var mockSearchCandidateService = new Mock<ISearchCandidateService>();
            mockSearchCandidateService.Setup(x => x.SearchCandidates(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<SearchCandidateViewModel> { new SearchCandidateViewModel() });

            var controller = CreateController(searchCandidateService: mockSearchCandidateService);

            var result = await controller.SearchCandidate("keyword", "location");
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("SearchResultsCandidates", viewResult.ViewName);
            Assert.NotNull(viewResult.Model);
        }

        [Fact]
        public async Task SearchForJobsAndAnalyzeCVAgainstJob_ReturnsViewWithResults()
        {
            var mockUserPropertiesService = new Mock<IUserPropertiesService>();
            mockUserPropertiesService.Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(new User
                {
                    Id = 1,
                    CVPath = "cv.pdf",
                    Username = "testuser",
                    PasswordHash = "hash",
                    Email = "test@example.com",
                    Location = "Testville"
                });

            var mockWorkflow = new Mock<ISearchWorkflowService>();
            mockWorkflow.Setup(x => x.SearchAndAttachMetaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<JobSearchResultViewModel>
                {
                    new JobSearchResultViewModel
                    {
                        Job = new JBDH.Models.Job.Job { JobId = 1 }
                    }
                });

            var mockUserAIResponseService = new Mock<IUserAIResponseService>();
            mockUserAIResponseService.Setup(x => x.GetLatestUserAIResponseAsync(It.IsAny<int>()))
                .ReturnsAsync(new JBDH.Models.AI.UserAIResponse
                {
                    ResponseText = "{\"BestJobTitlesForUser\":[\"Developer\"]}",
                    User = new User
                    {
                        Id = 1,
                        CVPath = "cv.pdf",
                        Username = "testuser",
                        PasswordHash = "hash",
                        Email = "test@example.com",
                        Location = "Testville"
                    }
                });

            var mockJobAIResponseService = new Mock<IJobAIResponseService>();
            mockJobAIResponseService.Setup(x => x.GetLatestJobAIResponseAsync(It.IsAny<int>()))
                .ReturnsAsync(new JBDH.Models.AI.JobAIResponse { ResponseText = "{\"JobTitle\":[\"Developer\"]}" });

            var mockMatchingAlgorithm = new Mock<IMatchingAlgorithmCVJobService>();
            mockMatchingAlgorithm.Setup(x => x.CalculateJobTitleMatchPercentage(It.IsAny<JobAdAIResponseViewModel>(), It.IsAny<JBDH.Models.User.CVAnalysisResult>()))
                .Returns(100);

            var controller = CreateController(
                userPropertiesService: mockUserPropertiesService,
                searchWorkflowService: mockWorkflow,
                userAIResponseService: mockUserAIResponseService,
                jobAIResponseService: mockJobAIResponseService,
                matchingAlgorithmCVJobService: mockMatchingAlgorithm
            );
            SetUserContext(controller);

            var result = await controller.SearchForJobsAndAnalyzeCVAgainstJob("desc", "loc");
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("SearchResultsJobs", viewResult.ViewName);
            Assert.NotNull(viewResult.Model);
            Assert.Equal(100, viewResult.ViewData["JobTitleMatchPercent"]);
        }

        [Fact]
        public async Task SearchUsers_ReturnsViewWithUsers()
        {
            var mockUserPropertiesService = new Mock<IUserPropertiesService>();
            mockUserPropertiesService.Setup(x => x.SearchUsersAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<User>
                {
                    new User
                    {
                        Id = 2,
                        CVPath = "cv2.pdf",
                        Username = "anotheruser",
                        PasswordHash = "hash2",
                        Email = "another@example.com",
                        Location = "Anotherville"
                    }
                });

            var controller = CreateController(userPropertiesService: mockUserPropertiesService);

            var result = await controller.SearchUsers("query");
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
        }
    }
}