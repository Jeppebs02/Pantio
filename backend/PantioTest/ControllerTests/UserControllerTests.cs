using System.Security.Claims;
using Microsoft.AspNetCore.Http;
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

    private void SetUserClaims(params Claim[] claims)
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"))
            }
        };
    }

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
    [Category("BR-01")]
    public async Task Register_ValidSecretAndDto_Returns200WithUserDto()
    {
        #region Arrange
        var dto = new CreateUserDto("test@example.com", "auth0|abc");
        var userDto = new UserDto(Guid.NewGuid(), "test@example.com", false, null);
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
    [Category("BR-01")]
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
    [Category("BR-01")]
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
    [Category("BR-02")]
    public async Task EnsureUser_AuthenticatedSubMatchesDto_Returns200WithUserDto()
    {
        #region Arrange
        var dto = new CreateUserDto("test@example.com", "auth0|abc");
        var userDto = new UserDto(Guid.NewGuid(), dto.Email, false, null);
        SetUserClaims(new Claim("sub", dto.Auth0Sub));
        _serviceMock
            .Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);
        #endregion

        #region Act
        var result = await _controller.EnsureUser(dto, CancellationToken.None);
        #endregion

        #region Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(userDto));
        #endregion
    }

    [Test]
    [Category("BR-02")]
    public async Task EnsureUser_MissingSub_Returns401()
    {
        #region Arrange
        var dto = new CreateUserDto("test@example.com", "auth0|abc");
        SetUserClaims();
        #endregion

        #region Act
        var result = await _controller.EnsureUser(dto, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        #endregion
    }

    [Test]
    [Category("BR-02")]
    public async Task EnsureUser_MismatchedSub_Returns403()
    {
        #region Arrange
        var dto = new CreateUserDto("test@example.com", "auth0|abc");
        SetUserClaims(new Claim("sub", "auth0|different"));
        #endregion

        #region Act
        var result = await _controller.EnsureUser(dto, CancellationToken.None);
        #endregion

        #region Assert
        Assert.That(result, Is.InstanceOf<ForbidResult>());
        #endregion
    }

    [Test]
    [Category("BR-04")]
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
    [Category("BR-04")]
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
