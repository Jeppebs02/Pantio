using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PantioAPI.Options;
using PantioAPI.Services;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Repository;

namespace PantioTest.ServiceTests;

public class RecipeSuggestionServiceTests
{
    private Mock<IInventoryItemRepository> _itemRepoMock = null!;
    private Mock<IRecipeRepository> _recipeRepoMock = null!;
    private Mock<IHttpClientFactory> _httpClientFactoryMock = null!;
    private RecipeSuggestionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _itemRepoMock = new Mock<IInventoryItemRepository>();
        _recipeRepoMock = new Mock<IRecipeRepository>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        var options = Options.Create(new GeminiOptions { ApiKey = "test-key" });

        _service = new RecipeSuggestionService(
            _itemRepoMock.Object,
            _recipeRepoMock.Object,
            _httpClientFactoryMock.Object,
            options,
            Mock.Of<ILogger<RecipeSuggestionService>>());
    }

    private static InventoryItem MakeItem(string name, decimal qty = 2m) => new()
    {
        Id = Guid.NewGuid(),
        InventoryId = Guid.NewGuid(),
        ProductName = name,
        Quantity = qty,
        QuantityUnit = null,
        Status = InventoryStatus.Available,
        AddedVia = AddedVia.Manual,
        AddedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        RowVersion = 0
    };

    private static HttpClient MakeFakeHttpClient(string geminiJson)
    {
        var responseBody = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[] { new { text = geminiJson } }
                    }
                }
            }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(responseBody));

        return new HttpClient(handler);
    }

    private static string BuildGeminiRecipesJson(int count = 3) =>
        JsonSerializer.Serialize(new
        {
            recipes = Enumerable.Range(1, count).Select(i => new
            {
                name = $"Recipe {i}",
                description = $"Description {i}",
                instructions = $"1. Step one\n2. Step two",
                portions = 4.0f,
                ingredients = new[]
                {
                    new { productName = "Chicken Breast", quantity = 300f, unit = "g" },
                    new { productName = "Salt", quantity = 1f, unit = (string)"" }
                }
            })
        });

    [Test]
    [Category("FR-01")]
    public async Task GetSuggestionsAsync_EmptyItemIds_ReturnsEmptyList()
    {
        #region Arrange
        var request = new RecipeSuggestionRequestDto([]);
        #endregion

        #region Act
        var result = await _service.GetSuggestionsAsync(Guid.NewGuid(), request);
        #endregion

        #region Assert
        Assert.That(result.Suggestions, Is.Empty);
        _itemRepoMock.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        #endregion
    }

    [Test]
    [Category("FR-01")]
    public async Task GetSuggestionsAsync_NoItemsFoundInRepository_ReturnsEmptyList()
    {
        #region Arrange
        var request = new RecipeSuggestionRequestDto([Guid.NewGuid()]);
        _itemRepoMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        #endregion

        #region Act
        var result = await _service.GetSuggestionsAsync(Guid.NewGuid(), request);
        #endregion

        #region Assert
        Assert.That(result.Suggestions, Is.Empty);
        #endregion
    }

    [Test]
    [Category("FR-01")]
    public async Task GetSuggestionsAsync_ValidItems_SavesRecipesToDb()
    {
        #region Arrange
        var item = MakeItem("Chicken Breast");
        var request = new RecipeSuggestionRequestDto([item.Id]);

        _itemRepoMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        _httpClientFactoryMock
            .Setup(f => f.CreateClient("Gemini"))
            .Returns(MakeFakeHttpClient(BuildGeminiRecipesJson(3)));

        _recipeRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recipe r, CancellationToken _) => r);
        #endregion

        #region Act
        var result = await _service.GetSuggestionsAsync(Guid.NewGuid(), request);
        #endregion

        #region Assert
        Assert.That(result.Suggestions.Count(), Is.EqualTo(3));
        _recipeRepoMock.Verify(r => r.CreateAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        #endregion
    }

    [Test]
    [Category("FR-01")]
    public async Task GetSuggestionsAsync_AllThreeSavedRecipes_ShareSameBatchId()
    {
        #region Arrange
        var item = MakeItem("Tomato");
        var request = new RecipeSuggestionRequestDto([item.Id]);
        var capturedBatchIds = new List<Guid?>();

        _itemRepoMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        _httpClientFactoryMock
            .Setup(f => f.CreateClient("Gemini"))
            .Returns(MakeFakeHttpClient(BuildGeminiRecipesJson(3)));

        _recipeRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()))
            .Callback<Recipe, CancellationToken>((recipe, _) => capturedBatchIds.Add(recipe.SuggestionBatchId))
            .ReturnsAsync((Recipe r, CancellationToken _) => r);
        #endregion

        #region Act
        await _service.GetSuggestionsAsync(Guid.NewGuid(), request);
        #endregion

        #region Assert
        Assert.That(capturedBatchIds, Has.Count.EqualTo(3));
        Assert.That(capturedBatchIds.Distinct().Count(), Is.EqualTo(1));
        Assert.That(capturedBatchIds[0], Is.Not.Null);
        #endregion
    }

    [Test]
    [Category("FR-01")]
    public async Task GetSuggestionsAsync_IngredientMatchesInventoryItem_SetsInventoryItemId()
    {
        #region Arrange
        var item = MakeItem("Chicken Breast");
        var request = new RecipeSuggestionRequestDto([item.Id]);
        Recipe? capturedRecipe = null;

        _itemRepoMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        _httpClientFactoryMock
            .Setup(f => f.CreateClient("Gemini"))
            .Returns(MakeFakeHttpClient(BuildGeminiRecipesJson(1)));

        _recipeRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()))
            .Callback<Recipe, CancellationToken>((r, _) => capturedRecipe = r)
            .ReturnsAsync((Recipe r, CancellationToken _) => r);
        #endregion

        #region Act
        await _service.GetSuggestionsAsync(Guid.NewGuid(), request);
        #endregion

        #region Assert
        Assert.That(capturedRecipe, Is.Not.Null);
        var chickenEntry = capturedRecipe!.Entries.First(e => e.ProductName == "Chicken Breast");
        Assert.That(chickenEntry.InventoryItemId, Is.EqualTo(item.Id));
        #endregion
    }

    [Test]
    [Category("FR-01")]
    public async Task GetSuggestionsAsync_UnmatchedIngredient_HasNullInventoryItemId()
    {
        #region Arrange
        var item = MakeItem("Chicken Breast");
        var request = new RecipeSuggestionRequestDto([item.Id]);
        Recipe? capturedRecipe = null;

        _itemRepoMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        _httpClientFactoryMock
            .Setup(f => f.CreateClient("Gemini"))
            .Returns(MakeFakeHttpClient(BuildGeminiRecipesJson(1)));

        _recipeRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()))
            .Callback<Recipe, CancellationToken>((r, _) => capturedRecipe = r)
            .ReturnsAsync((Recipe r, CancellationToken _) => r);
        #endregion

        #region Act
        await _service.GetSuggestionsAsync(Guid.NewGuid(), request);
        #endregion

        #region Assert
        Assert.That(capturedRecipe, Is.Not.Null);
        var saltEntry = capturedRecipe!.Entries.First(e => e.ProductName == "Salt");
        Assert.That(saltEntry.InventoryItemId, Is.Null);
        #endregion
    }
}

file class FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });
}
