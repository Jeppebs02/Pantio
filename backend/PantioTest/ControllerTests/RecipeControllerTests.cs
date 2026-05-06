using Microsoft.AspNetCore.Mvc;
using Moq;
using PantioAPI.Controllers;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ControllerTests;

public class RecipeControllerTests
{
    private Mock<IRecipeService> _serviceMock = null!;
    private RecipeController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IRecipeService>();
        _controller = new RecipeController(_serviceMock.Object);
    }

    [Test]
    public async Task Complete_ExistingRecipe_Returns204()
    {
        #region Arrange
        var recipeId = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.CompleteAsync(recipeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        #endregion

        #region Act
        var result = await _controller.Complete(recipeId, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
        #endregion
    }

    [Test]
    public async Task Complete_NonExistentRecipe_Returns404()
    {
        #region Arrange
        _serviceMock
            .Setup(s => s.CompleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        #endregion

        #region Act
        var result = await _controller.Complete(Guid.NewGuid(), CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        #endregion
    }

    [Test]
    public async Task Link_ExistingRecipe_Returns200WithDto()
    {
        #region Arrange
        var recipeId = Guid.NewGuid();
        var dto = new RecipeSuggestionDto(recipeId, "Pasta", "", "", 2f, []);
        _serviceMock
            .Setup(s => s.LinkToInventoryAsync(recipeId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        #endregion

        #region Act
        var result = await _controller.Link(recipeId, new RecipeLinkRequestDto(Guid.NewGuid()), CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(dto));
        #endregion
    }

    [Test]
    public async Task Link_NonExistentRecipe_Returns404()
    {
        #region Arrange
        _serviceMock
            .Setup(s => s.LinkToInventoryAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecipeSuggestionDto?)null);
        #endregion

        #region Act
        var result = await _controller.Link(Guid.NewGuid(), new RecipeLinkRequestDto(Guid.NewGuid()), CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        #endregion
    }
}
