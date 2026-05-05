using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using PantioAPI.Controllers;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ControllerTests;

public class UserControllerTests
{
    private Mock<IUserService> _serviceMock = null!;
    private IConfiguration _config = null!;
    private UserController _controller = null!;

    private const string ValidSecret = "super-secret";

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IUserService>();
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth0:RegistrationSecret"] = ValidSecret
            })
            .Build();
        _controller = new UserController(_serviceMock.Object, _config);
    }

    [Test]
    public async Task Register_ValidSecretAndDto_Returns200WithUserDto()
    {
        #region Arrange
        var dto = new CreateUserDto("test@example.com", "auth0|abc");
        var userDto = new UserDto(Guid.NewGuid(), "test@example.com", false);
        _serviceMock
            .Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);
        #endregion

        #region Act
        var result = await _controller.Register(dto, ValidSecret, CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(userDto));
        #endregion
    }

    [Test]
    public async Task Register_WrongSecret_Returns401()
    {
        #region Arrange
        var dto = new CreateUserDto("test@example.com", "auth0|abc");
        #endregion

        #region Act
        var result = await _controller.Register(dto, "wrong-secret", CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        #endregion
    }

    [Test]
    public async Task Register_MissingSecret_Returns401()
    {
        #region Arrange
        var dto = new CreateUserDto("test@example.com", "auth0|abc");
        #endregion

        #region Act
        var result = await _controller.Register(dto, null, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        #endregion
    }

    [Test]
    public async Task Delete_ExistingUser_Returns204()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.DeleteAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        #endregion

        #region Act
        var result = await _controller.Delete(userId, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
        #endregion
    }

    [Test]
    public async Task Delete_NonExistentUser_Returns404()
    {
        #region Arrange
        _serviceMock
            .Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        #endregion

        #region Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        #endregion
    }
}
