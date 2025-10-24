/*
using Xunit;
using Moq;
using JBDH.Controllers;
using JBDH.Services.DataFlow;
using JBDH.Services.Reporting;
using JBDH.Models.Core;
using JBDH.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using JBDH.Services.AICommunications;

public class HomeControllerTests
{
    [Fact]
    public async Task Index_ReturnsViewResult_WithListDataFlowViewModel()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
        var context = new ApplicationDbContext(options);

        var mockEnv = new Mock<IWebHostEnvironment>();
        var mockRapportUserService = new Mock<IRapportUserService>();
        var mockAIRequestBuilderService = new Mock<IAIRequestBuilderService>();

        var fakeDataFlowService = new FakeDataFlowService(context);

        var controller = new HomeController(
            context,
            mockEnv.Object,
            fakeDataFlowService,
            mockRapportUserService.Object,
            mockAIRequestBuilderService.Object
        );

        // Set up a fake user
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        }, "mock"));

        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        // Mock Users.AnyAsync to return true
        context.Users.Add(new JBDH.Models.User.User
        {
            Id = 1,
            PasswordHash = "hash",
            Email = "test@example.com",
            Location = "Test City"
        });
        context.SaveChanges();

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ListDataFlowViewModel>(viewResult.Model);
    }

    [Fact]
    public void HomeController_Constructor_ShouldInitialize()
    {
        var mockContext = new Mock<ApplicationDbContext>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        var mockDataFlowService = new Mock<DataFlowService>();
        var mockRapportUserService = new Mock<IRapportUserService>();
        var mockAIRequestBuilderService = new Mock<IAIRequestBuilderService>();

        var controller = new HomeController(
            mockContext.Object,
            mockEnv.Object,
            mockDataFlowService.Object,
            mockRapportUserService.Object,
            mockAIRequestBuilderService.Object
        );

        Assert.NotNull(controller);
    }
}

public class FakeDataFlowService : DataFlowService
{
    public FakeDataFlowService(ApplicationDbContext context) : base(context) { }
    public new List<JBDH.Models.Dataflow.DataFlow> GetAllRecursive() => new();
}
*/