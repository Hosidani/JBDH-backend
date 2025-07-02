using Xunit;
using Moq;
using JBDH.Controllers;
using JBDH.Models.Kanban;
using JBDH.Services.BoardDataService;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using JBDH.Models.Core;
using Microsoft.EntityFrameworkCore;

public class BoardsControllerTests
{
    [Fact]
    public async Task Create_Post_ValidModel_RedirectsToIndex()
    {
        // Arrange
        var mockBoardService = new Mock<IBoardService>();
        mockBoardService.Setup(s => s.CreateAsync(It.IsAny<Board>())).Returns(Task.CompletedTask);
        var controller = new BoardsController(mockBoardService.Object);
        var board = new Board { Name = "Test Board" };

        // Set up fake user with NameIdentifier claim
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        }, "mock"));

        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        // Set up TempData
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        // Act
        var result = await controller.Create(board) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
    }

    [Fact]
    public async Task Edit_Post_ValidModel_RedirectsToIndex()
    {
        // Arrange
        var mockBoardService = new Mock<IBoardService>();
        mockBoardService.Setup(s => s.UpdateAsync(It.IsAny<Board>())).Returns(Task.CompletedTask);
        var controller = new BoardsController(mockBoardService.Object);
        var board = new Board { Id = 1, Name = "Updated Board" };

        // Set up fake user with NameIdentifier claim
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        }, "mock"));

        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        // Act
        var result = await controller.Edit(1, board) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
    }

    [Fact]
    public async Task DeleteConfirmed_ValidId_RedirectsToIndex()
    {
        // Arrange
        var mockBoardService = new Mock<IBoardService>();
        mockBoardService.Setup(s => s.DeleteAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        var controller = new BoardsController(mockBoardService.Object);

        // Act
        var result = await controller.DeleteConfirmed(1) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
    }
}