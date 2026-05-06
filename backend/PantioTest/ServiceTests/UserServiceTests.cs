using Moq;
using PantioAPI.Services;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ServiceTests;

public class UserServiceTests
{
    private Mock<IUserRepository> _repositoryMock = null!;
    private Mock<IAuth0ManagementService> _auth0ManagementServiceMock = null!;
    private UserService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _auth0ManagementServiceMock = new Mock<IAuth0ManagementService>();
        _service = new UserService(_repositoryMock.Object, _auth0ManagementServiceMock.Object);
    }

    [Test]
    public async Task CreateAsync_ValidDto_ReturnsMappedDto()
    {
        #region Arrange
        var dto = new CreateUserDto("test@example.com", "auth0|abc123");
        var createdUser = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            Auth0Sub = dto.Auth0Sub,
            OnboardingDone = false
        };
        _repositoryMock
            .Setup(r => r.GetByAuth0SubAsync(dto.Auth0Sub, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);
        #endregion

        #region Act
        var result = await _service.CreateAsync(dto);
        #endregion

        #region Assert
        Assert.That(result.Id, Is.EqualTo(createdUser.Id));
        Assert.That(result.Email, Is.EqualTo(createdUser.Email));
        Assert.That(result.OnboardingDone, Is.EqualTo(createdUser.OnboardingDone));
        _repositoryMock.Verify(r => r.GetByAuth0SubAsync(dto.Auth0Sub, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<User>(u => u.Email == dto.Email && u.Auth0Sub == dto.Auth0Sub),
            It.IsAny<CancellationToken>()), Times.Once);
        #endregion
    }

    [Test]
    public async Task CreateAsync_ExistingUserWithSub_ReturnsExistingUserWithoutCreatingDuplicate()
    {
        #region Arrange
        var dto = new CreateUserDto("test@example.com", "auth0|abc123");
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            Auth0Sub = dto.Auth0Sub,
            OnboardingDone = true
        };
        _repositoryMock
            .Setup(r => r.GetByAuth0SubAsync(dto.Auth0Sub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        #endregion

        #region Act
        var result = await _service.CreateAsync(dto);
        #endregion

        #region Assert
        Assert.That(result.Id, Is.EqualTo(existingUser.Id));
        Assert.That(result.Email, Is.EqualTo(existingUser.Email));
        Assert.That(result.OnboardingDone, Is.EqualTo(existingUser.OnboardingDone));
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        #endregion
    }

    [Test]
    public async Task DeleteAsync_ExistingUser_DeletesFromAuth0ThenRepositoryAndReturnsTrue()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Auth0Sub = "auth0|abc123"
        };
        _repositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _repositoryMock
            .Setup(r => r.DeleteAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        #endregion

        #region Act
        var result = await _service.DeleteAsync(userId);
        #endregion

        #region Assert
        Assert.That(result, Is.True);
        _auth0ManagementServiceMock.Verify(s => s.DeleteUserAsync(user.Auth0Sub, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        #endregion
    }

    [Test]
    public async Task DeleteAsync_NonExistentUser_ReturnsFalseWithoutCallingAuth0()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        #endregion

        #region Act
        var result = await _service.DeleteAsync(userId);
        #endregion

        #region Assert
        Assert.That(result, Is.False);
        _auth0ManagementServiceMock.Verify(s => s.DeleteUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        #endregion
    }
}
