/*
using System.Threading.Tasks;
using JBDH.Controllers;
using JBDH.Services.UserManagement;
using JBDH.Services.Friendship;
using JBDH.Models.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace JBDH.Tests.Controllers
{
    public class FriendsControllerTests
    {
        [Fact]
        public async Task SendRequest_HappyPath_RedirectsToSuccessPage()
        {
            // Arrange
            var mockUserService = new Mock<IUserPropertiesService>();
            var mockFriendService = new Mock<IFriendService>();

            var fakeUser = new JBDH.Models.User.User
            {
                Id = 42,
                PasswordHash = "hash",
                Email = "test@example.com",
                Location = "Testville"
            };
            mockUserService.Setup(s => s.GetCurrentUserAsync())
                .ReturnsAsync(fakeUser);

            mockFriendService.Setup(s => s.SendFriendRequestAsync(42, 99))
                .ReturnsAsync(new ServiceResult { Success = true });

            var controller = new FriendsController(mockUserService.Object, mockFriendService.Object);

            // Act
            var result = await controller.SendRequest("99");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SuccessPage", redirect.ActionName);
            Assert.Equal("Settings", redirect.ControllerName);
            Assert.Equal("Friend request sent!", redirect.RouteValues["message"]);
        }
    }
}
*/