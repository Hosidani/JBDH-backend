using Xunit;
using Moq;
using JBDH.Controllers;
using JBDH.Models.Kanban;
using JBDH.Services.BoardDataService;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class BoardDataServiceTests
{
    [Fact]
    public async Task Details_ReturnsBoardWithListsAndCards()
    {
        // Arrange
        var mockService = new Mock<IBoardService>();
        var testBoard = new Board
        {
            Id = 1,
            Name = "Test Board",
            Lists = new List<KanbanList>
            {
                new KanbanList
                {
                    Id = 10,
                    Name = "Test List",
                    Cards = new List<Card>
                    {
                        new Card { Id = 100, Title = "Test Card" }
                    }
                }
            }
        };
        mockService.Setup(s => s.GetAsync(1)).ReturnsAsync(testBoard);

        var controller = new BoardsController(mockService.Object);

        // Act
        var result = await controller.Details(1) as ViewResult;
        var model = result?.Model as Board;

        // Assert
        Assert.NotNull(model);
        Assert.Equal(1, model.Id);
        Assert.Single(model.Lists);
        Assert.Single(model.Lists.First().Cards);
        Assert.Equal("Test Card", model.Lists.First().Cards.First().Title);
    }
} 