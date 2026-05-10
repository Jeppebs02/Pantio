using Microsoft.AspNetCore.Mvc;
using Moq;
using PantioAPI.Controllers;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ControllerTests;

public class ProductsControllerTests
{
    private Mock<IProductCacheService> _cacheMock = null!;
    private Mock<IOpenFoodFactsService> _offMock = null!;
    private ProductsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _cacheMock = new Mock<IProductCacheService>();
        _offMock = new Mock<IOpenFoodFactsService>();
        _controller = new ProductsController(_cacheMock.Object, _offMock.Object);
    }

    private static OffProductData MakeProduct() =>
        new("Arla Letmælk", ["en:milks", "en:dairy"], null);

    [Test]
    public async Task GetByEan_CachedProduct_ReturnsOkWithoutCallingOff()
    {
        #region Arrange
        var ean = "5701234567890";
        var product = MakeProduct();
        _cacheMock
            .Setup(c => c.GetAsync(ean, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        #endregion

        #region Act
        var result = await _controller.GetByEan(ean, CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(product));
        _offMock.Verify(o => o.GetByEanAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        #endregion
    }

    [Test]
    public async Task GetByEan_CacheMiss_CallsOffAndCachesResult()
    {
        #region Arrange
        var ean = "5701234567890";
        var product = MakeProduct();
        _cacheMock
            .Setup(c => c.GetAsync(ean, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OffProductData?)null);
        _offMock
            .Setup(o => o.GetByEanAsync(ean, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        #endregion

        #region Act
        var result = await _controller.GetByEan(ean, CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(product));
        _cacheMock.Verify(c => c.SetAsync(ean, product, It.IsAny<CancellationToken>()), Times.Once);
        #endregion
    }

    [Test]
    public async Task GetByEan_NotFoundInCacheOrOff_Returns404()
    {
        #region Arrange
        var ean = "0000000000000";
        _cacheMock
            .Setup(c => c.GetAsync(ean, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OffProductData?)null);
        _offMock
            .Setup(o => o.GetByEanAsync(ean, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OffProductData?)null);
        #endregion

        #region Act
        var result = await _controller.GetByEan(ean, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<OffProductData>(), It.IsAny<CancellationToken>()), Times.Never);
        #endregion
    }
}
