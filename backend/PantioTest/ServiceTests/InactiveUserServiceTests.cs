using Microsoft.Extensions.Logging;
using Moq;
using PantioAPI.Services;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;
using PantioClassLibrary.Interfaces.Services;

namespace PantioTest.ServiceTests;

public class InactiveUserServiceTests
{
    private Mock<IUserRepository> _repositoryMock = null!;
    private Mock<IAuth0ManagementService> _auth0Mock = null!;
    private InactiveUserService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _auth0Mock = new Mock<IAuth0ManagementService>();
        _service = new InactiveUserService(
            _repositoryMock.Object,
            _auth0Mock.Object,
            Mock.Of<ILogger<InactiveUserService>>());
    }

    private static User MakeUser(DateTime? lastActivityAt = null, DateTime? createdAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        Auth0Sub = $"auth0|{Guid.NewGuid():N}",
        CreatedAt = createdAt ?? DateTime.UtcNow.AddMonths(-1),
        LastActivityAt = lastActivityAt
    };

    [Test]
    public async Task RunCheckAsync_UserInactiveOver11Months_StampsDeletionWarning()
    {
        #region Arrange
        var user = MakeUser(lastActivityAt: DateTime.UtcNow.AddMonths(-11).AddDays(-1));
        _repositoryMock
            .Setup(r => r.GetUsersToWarnAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);
        _repositoryMock
            .Setup(r => r.GetUsersToDeleteAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        #endregion

        #region Act
        await _service.RunCheckAsync();
        #endregion

        #region Assert
        _repositoryMock.Verify(
            r => r.SetDeletionWarningSentAsync(user.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
        #endregion
    }

    [Test]
    public async Task RunCheckAsync_UserAlreadyWarned_DoesNotStampWarningAgain()
    {
        #region Arrange
        _repositoryMock
            .Setup(r => r.GetUsersToWarnAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repositoryMock
            .Setup(r => r.GetUsersToDeleteAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        #endregion

        #region Act
        await _service.RunCheckAsync();
        #endregion

        #region Assert
        _repositoryMock.Verify(
            r => r.SetDeletionWarningSentAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        #endregion
    }

    [Test]
    public async Task RunCheckAsync_UserInactiveOver12Months_DeletesFromAuth0AndRepository()
    {
        #region Arrange
        var user = MakeUser(lastActivityAt: DateTime.UtcNow.AddMonths(-12).AddDays(-1));
        _repositoryMock
            .Setup(r => r.GetUsersToWarnAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repositoryMock
            .Setup(r => r.GetUsersToDeleteAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);
        _repositoryMock
            .Setup(r => r.DeleteAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        #endregion

        #region Act
        await _service.RunCheckAsync();
        #endregion

        #region Assert
        _auth0Mock.Verify(s => s.DeleteUserAsync(user.Auth0Sub, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        #endregion
    }

    [Test]
    public async Task RunCheckAsync_Auth0DeleteFails_ContinuesBatchAndLogsError()
    {
        #region Arrange
        var failingUser = MakeUser(lastActivityAt: DateTime.UtcNow.AddMonths(-13));
        var successUser = MakeUser(lastActivityAt: DateTime.UtcNow.AddMonths(-13));
        _repositoryMock
            .Setup(r => r.GetUsersToWarnAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repositoryMock
            .Setup(r => r.GetUsersToDeleteAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([failingUser, successUser]);
        _auth0Mock
            .Setup(s => s.DeleteUserAsync(failingUser.Auth0Sub, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Auth0 error"));
        _repositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        #endregion

        #region Act
        await _service.RunCheckAsync();
        #endregion

        #region Assert
        _auth0Mock.Verify(s => s.DeleteUserAsync(successUser.Auth0Sub, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(successUser.Id, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(failingUser.Id, It.IsAny<CancellationToken>()), Times.Never);
        #endregion
    }

    [Test]
    public async Task RunCheckAsync_UserInactiveLessThan11Months_IsUntouched()
    {
        #region Arrange
        _repositoryMock
            .Setup(r => r.GetUsersToWarnAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repositoryMock
            .Setup(r => r.GetUsersToDeleteAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        #endregion

        #region Act
        await _service.RunCheckAsync();
        #endregion

        #region Assert
        _auth0Mock.Verify(s => s.DeleteUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SetDeletionWarningSentAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        #endregion
    }
}
