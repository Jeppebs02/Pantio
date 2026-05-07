using Microsoft.AspNetCore.Mvc;
using Moq;
using PantioAPI.Controllers;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ControllerTests;

public class RecipeSuggestionControllerTests
{
    private Mock<IRecipeSuggestionService> _serviceMock = null!;
    private RecipeSuggestionController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IRecipeSuggestionService>();
        _controller = new RecipeSuggestionController(_serviceMock.Object);
    }

    private static RecipeSuggestionListDto MakeSuggestionList(int count = 3) =>
        new(Enumerable.Range(1, count).Select(i => new RecipeSuggestionDto(
            Guid.NewGuid(), $"Recipe {i}", "Description", "Instructions", 4f, [])));

    [Test]
    public async Task GetSuggestions_EmptyItemIds_Returns400()
    {
        #region Arrange
        var request = new RecipeSuggestionRequestDto([]);
        #endregion

        #region Act
        var result = await _controller.GetSuggestions(Guid.NewGuid(), request, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        #endregion
    }

    [Test]
    public async Task GetSuggestions_ValidRequest_Returns200WithSuggestions()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var request = new RecipeSuggestionRequestDto([Guid.NewGuid(), Guid.NewGuid()]);
        var suggestions = MakeSuggestionList(3);
        _serviceMock
            .Setup(s => s.GetSuggestionsAsync(userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestions);
        #endregion

        #region Act
        var result = await _controller.GetSuggestions(userId, request, CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(suggestions));
        #endregion
    }

    [Test]
    public async Task GetSuggestions_ValidRequest_CallsServiceWithCorrectUserId()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var request = new RecipeSuggestionRequestDto([Guid.NewGuid()]);
        _serviceMock
            .Setup(s => s.GetSuggestionsAsync(userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSuggestionList());
        #endregion

        #region Act
        await _controller.GetSuggestions(userId, request, CancellationToken.None);
        #endregion

        #region Assert
        _serviceMock.Verify(s => s.GetSuggestionsAsync(userId, request, It.IsAny<CancellationToken>()), Times.Once);
        #endregion
    }
}
