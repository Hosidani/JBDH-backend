/*
using Xunit;
using Moq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JBDH.Controllers;
using JBDH.Models.Kanban;
using JBDH.Services.BoardDataService;
using JBDH.Services.ListService;

namespace JBDH.Tests.Controllers
{
    public class CardsControllerTests
    {
        private readonly Mock<ICardService> _mockCardService;
        private readonly Mock<IListService> _mockListService;
        private readonly CardsController _controller;

        public CardsControllerTests()
        {
            _mockCardService = new Mock<ICardService>();
            _mockListService = new Mock<IListService>();
            _controller = new CardsController(_mockCardService.Object, _mockListService.Object);
        }

        [Fact]
        public async Task Create_GET_ReturnsViewWithCardModel()
        {
            // Arrange
            int testListId = 1;
            var testList = new KanbanList { Id = testListId, BoardId = 123, Name = "Test List" };
            _mockListService.Setup(s => s.GetAsync(testListId)).ReturnsAsync(testList);

            // Act
            var result = await _controller.Create(testListId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<Card>(viewResult.ViewData.Model);
            Assert.Equal(testListId, model.ListId);
            Assert.Equal(123, _controller.ViewBag.BoardId);
        }

        [Fact]
        public async Task Create_POST_ValidModel_RedirectsToBoardDetails()
        {
            // Arrange
            var cardToCreate = new Card { ListId = 1, Title = "New Test Card" };
            var parentList = new KanbanList { Id = 1, BoardId = 123, Name = "Parent List" };

            _mockListService.Setup(s => s.GetAsync(cardToCreate.ListId)).ReturnsAsync(parentList);
            _mockCardService.Setup(s => s.CreateAsync(It.IsAny<Card>())).ReturnsAsync(cardToCreate);

            // Act
            var result = await _controller.Create(cardToCreate);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectToActionResult.ActionName);
            Assert.Equal("Boards", redirectToActionResult.ControllerName);
            Assert.Equal(123, Convert.ToInt32(redirectToActionResult.RouteValues["id"]));
            _mockCardService.Verify(s => s.CreateAsync(cardToCreate), Times.Once);
        }

        [Fact]
        public async Task Create_POST_InvalidModel_ReturnsView()
        {
            // Arrange
            var cardToCreate = new Card { ListId = 1, Title = string.Empty }; // Title is required, use empty string for invalid model
            _controller.ModelState.AddModelError("Title", "The Title field is required.");

            // Act
            var result = await _controller.Create(cardToCreate);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(cardToCreate, viewResult.Model);
            _mockCardService.Verify(s => s.CreateAsync(It.IsAny<Card>()), Times.Never);
        }

        [Fact]
        public async Task Create_POST_ListNotFound_ReturnsNotFound()
        {
            // Arrange
            var cardToCreate = new Card { ListId = 999, Title = "Test" };
            _mockListService.Setup(s => s.GetAsync(cardToCreate.ListId)).ReturnsAsync((KanbanList)null);

            // Act
            var result = await _controller.Create(cardToCreate);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            _mockCardService.Verify(s => s.CreateAsync(It.IsAny<Card>()), Times.Never);
        }
    }
}
*/