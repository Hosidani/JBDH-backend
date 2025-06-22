using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using JBDH.Controllers;
using JBDH.Models.Core;
using JBDH.Models.ViewModels;
using JBDH.Models.Services;
using JBDH.Services.UserManagement;
using JBDH.Services.Organisation;
using JBDH.Services.Notifications;
using JBDH.Services.AICommunications;
using JBDH.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace JBDH.Tests.Controllers
{
    public class RegisterControllerTests : IDisposable
    {
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<RegisterController>> _mockLogger;
        private readonly Mock<IAIApiClient> _mockAIApiClient;
        private readonly Mock<IUserRegistrationService> _mockUserRegistrationService;
        private readonly Mock<IRegisterOrganisationService> _mockRegisterOrganisationService;
        private readonly Mock<IUserVerificationService> _mockUserVerificationService;
        private readonly Mock<IUserAIResponseService> _mockUserAIResponseService;
        private readonly ApplicationDbContext _context;
        private readonly RegisterController _controller;

        public RegisterControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Setup mocks
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<RegisterController>>();
            _mockAIApiClient = new Mock<IAIApiClient>();
            _mockUserRegistrationService = new Mock<IUserRegistrationService>();
            _mockRegisterOrganisationService = new Mock<IRegisterOrganisationService>();
            _mockUserVerificationService = new Mock<IUserVerificationService>();
            _mockUserAIResponseService = new Mock<IUserAIResponseService>();

            // Setup controller
            _controller = new RegisterController(
                _mockEmailService.Object,
                _context,
                _mockLogger.Object,
                _mockAIApiClient.Object,
                _mockUserRegistrationService.Object,
                _mockRegisterOrganisationService.Object,
                _mockUserVerificationService.Object,
                _mockUserAIResponseService.Object);

            // Setup HTTP context
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public void Register_ShouldReturnView()
        {
            // Act
            var result = _controller.Register();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // Should return default view
        }

        [Fact]
        public void RegisterUser_Get_ShouldReturnViewWithModel()
        {
            // Act
            var result = _controller.RegisterUser();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RegisterUserViewModel>(viewResult.Model);
            Assert.NotNull(model.AvailableRoles);
            Assert.NotNull(model.SelectedRoles);
            Assert.Contains("Manager", model.AvailableRoles);
            Assert.Contains("Recruiter", model.AvailableRoles);
        }

        [Fact]
        public async Task RegisterUser_Post_WithValidModel_ShouldReturnCheckYourEmailView()
        {
            // Arrange
            var model = TestDataBuilder.CreateValidRegisterUserViewModel();
            _mockUserRegistrationService.Setup(x => x.RegisterUserAsync(model, It.IsAny<HttpContext>(), It.IsAny<IUrlHelper>()))
                .ReturnsAsync(ServiceResult.SuccessResult());

            // Act
            var result = await _controller.RegisterUser(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("CheckYourEmail", viewResult.ViewName);
            Assert.Equal(model.Email, viewResult.ViewData["Email"]);
        }

        [Fact]
        public async Task RegisterUser_Post_WithInvalidModel_ShouldReturnViewWithError()
        {
            // Arrange
            var model = TestDataBuilder.CreateValidRegisterUserViewModel();
            var errorMessage = "Registration failed";
            _mockUserRegistrationService.Setup(x => x.RegisterUserAsync(model, It.IsAny<HttpContext>(), It.IsAny<IUrlHelper>()))
                .ReturnsAsync(ServiceResult.Failure(errorMessage));

            // Act
            var result = await _controller.RegisterUser(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // Should return default view
            Assert.Equal(errorMessage, viewResult.ViewData["ErrorMessage"]);
            Assert.Same(model, viewResult.Model);
        }

        [Fact]
        public void RegisterOrganization_Get_ShouldReturnView()
        {
            // Act
            var result = _controller.RegisterOrganization();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public async Task RegisterOrganization_Post_WithValidName_ShouldRedirectToHome()
        {
            // Arrange
            var organisationName = "Test Organisation";
            var managerUsername = "testmanager";
            
            // Setup user identity
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, managerUsername)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;

            var registerResult = new RegisterOrganisationResult { Success = true };
            _mockRegisterOrganisationService.Setup(x => x.RegisterAsync(organisationName, managerUsername))
                .ReturnsAsync(registerResult);

            // Act
            var result = await _controller.RegisterOrganization(organisationName);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task RegisterOrganization_Post_WithInvalidName_ShouldReturnViewWithError()
        {
            // Arrange
            var organisationName = "Test Organisation";
            var managerUsername = "testmanager";
            var errorMessage = "Registration failed";
            
            // Setup user identity
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, managerUsername)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;

            var registerResult = new RegisterOrganisationResult { Success = false, ErrorMessage = errorMessage };
            _mockRegisterOrganisationService.Setup(x => x.RegisterAsync(organisationName, managerUsername))
                .ReturnsAsync(registerResult);

            // Act
            var result = await _controller.RegisterOrganization(organisationName);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
            Assert.Equal(errorMessage, viewResult.ViewData["ErrorMessage"]);
        }

        [Fact]
        public async Task RegisterOrganization_Post_WithoutUserIdentity_ShouldReturnViewWithError()
        {
            // Arrange
            var organisationName = "Test Organisation";
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            // Act
            var result = await _controller.RegisterOrganization(organisationName);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
            Assert.Equal("You must be logged in to register an organisation.", viewResult.ViewData["ErrorMessage"]);
        }

        [Fact]
        public void VerifyEmail_WithValidToken_ShouldReturnVerificationSuccessView()
        {
            // Arrange
            var token = "valid-token";
            var verificationResult = new VerificationResult { Success = true };
            _mockUserVerificationService.Setup(x => x.VerifyEmail(token))
                .Returns(verificationResult);

            // Act
            var result = _controller.VerifyEmail(token);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("VerificationSuccess", viewResult.ViewName);
        }

        [Fact]
        public void VerifyEmail_WithInvalidToken_ShouldReturnVerificationFailedView()
        {
            // Arrange
            var token = "invalid-token";
            var errorMessage = "Invalid token";
            var verificationResult = new VerificationResult { Success = false, ErrorMessage = errorMessage };
            _mockUserVerificationService.Setup(x => x.VerifyEmail(token))
                .Returns(verificationResult);

            // Act
            var result = _controller.VerifyEmail(token);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("VerificationFailed", viewResult.ViewName);
            Assert.Equal(errorMessage, viewResult.ViewData["ErrorMessage"]);
        }

        [Fact]
        public async Task ResendVerificationEmail_WithValidEmail_ShouldRedirectToCheckYourEmail()
        {
            // Arrange
            var email = "test@example.com";
            var verificationResult = new VerificationResult { Success = true };
            _mockUserVerificationService.Setup(x => x.ResendVerificationEmailAsync(email, It.IsAny<IUrlHelper>(), It.IsAny<HttpRequest>()))
                .ReturnsAsync(verificationResult);

            // Setup TempData
            var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;

            // Act
            var result = await _controller.ResendVerificationEmail(email);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("CheckYourEmail", redirectResult.ActionName);
            Assert.Equal(email, _controller.TempData["Email"]);
        }

        [Fact]
        public async Task ResendVerificationEmail_WithInvalidEmail_ShouldReturnCheckYourEmailViewWithError()
        {
            // Arrange
            var email = "invalid@example.com";
            var errorMessage = "Email not found";
            var verificationResult = new VerificationResult { Success = false, ErrorMessage = errorMessage };
            _mockUserVerificationService.Setup(x => x.ResendVerificationEmailAsync(email, It.IsAny<IUrlHelper>(), It.IsAny<HttpRequest>()))
                .ReturnsAsync(verificationResult);

            // Act
            var result = await _controller.ResendVerificationEmail(email);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("CheckYourEmail", viewResult.ViewName);
            Assert.Equal(errorMessage, viewResult.ViewData["ErrorMessage"]);
            Assert.Equal(email, viewResult.ViewData["Email"]);
        }

        [Fact]
        public void CheckYourEmail_ShouldReturnView()
        {
            // Arrange
            var email = "test@example.com";
            var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            tempData["Email"] = email;
            _controller.TempData = tempData;

            // Act
            var result = _controller.CheckYourEmail();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(email, viewResult.ViewData["Email"]);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
} 