using Xunit;
using Moq;
using JBDH.Controllers;
using JBDH.Models.Kanban;
using JBDH.Services.ListService;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JBDH.Tests.Controllers
{
    public class ListsControllerTests
    {
        private readonly Mock<IListService> _mockListService;
        private readonly ListsController _controller;

        public ListsControllerTests()
        {
            _mockListService = new Mock<IListService>();
            _controller = new ListsController(_mockListService.Object);
        }

        [Fact]
        public void Create_Get_ReturnsViewWithKanbanList()
        {
            // Arrange
            int boardId = 1;

            // Act
            var result = _controller.Create(boardId) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<KanbanList>(result.Model);
            Assert.Equal(boardId, model.BoardId);
        }

        [Fact]
        public async Task Create_Post_ValidModel_RedirectsToBoardDetails()
        {
            // Arrange
            var kanbanList = new KanbanList { Name = "Test List", BoardId = 1 };
            _mockListService.Setup(s => s.CreateAsync(It.IsAny<KanbanList>())).ReturnsAsync(kanbanList);

            // Act
            var result = await _controller.Create(kanbanList) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Details", result.ActionName);
            Assert.Equal("Boards", result.ControllerName);
            Assert.Equal(kanbanList.BoardId, result.RouteValues["id"]);
            _mockListService.Verify(s => s.CreateAsync(kanbanList), Times.Once);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            var kanbanList = new KanbanList { Name = "Test List", BoardId = 1 };
            _controller.ModelState.AddModelError("Name", "Required");

            // Act
            var result = await _controller.Create(kanbanList) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<KanbanList>(result.Model);
            Assert.Equal(kanbanList.Name, model.Name);
            _mockListService.Verify(s => s.CreateAsync(It.IsAny<KanbanList>()), Times.Never);
        }
    }
} 