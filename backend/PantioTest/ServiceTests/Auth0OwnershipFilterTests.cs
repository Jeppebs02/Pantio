using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using PantioAPI.Filters;
using PantioClassLibrary.Entities;
using PantioClassLibrary.Interfaces.Repository;

namespace PantioTest.ServiceTests;

public class Auth0OwnershipFilterTests
{
    private Mock<IUserRepository> _repoMock = null!;
    private Auth0OwnershipFilter _filter = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<IUserRepository>();
        _filter = new Auth0OwnershipFilter(_repoMock.Object);
    }

    private static ActionExecutingContext BuildContext(
        string? sub,
        Guid? routeUserId = null,
        bool allowAnonymous = false)
    {
        var claims = sub is not null
            ? new[] { new Claim("sub", sub) }
            : Array.Empty<Claim>();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims))
        };

        if (allowAnonymous)
        {
            var endpointMetadata = new EndpointMetadataCollection(new AllowAnonymousAttribute());
            httpContext.SetEndpoint(new Endpoint(null, endpointMetadata, "test"));
        }

        var actionArguments = new Dictionary<string, object?>();
        if (routeUserId.HasValue)
            actionArguments["userId"] = routeUserId.Value;

        return new ActionExecutingContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            [],
            actionArguments,
            new object());
    }

    private static ActionExecutionDelegate NextDelegate() =>
        () => Task.FromResult(new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            [],
            new object()));

    [Test]
    public async Task OnActionExecutionAsync_AllowAnonymousEndpoint_SkipsValidation()
    {
        #region Arrange
        var context = BuildContext(sub: null, allowAnonymous: true);
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                [],
                new object()));
        };
        #endregion

        #region Act
        await _filter.OnActionExecutionAsync(context, next);
        #endregion

        #region Assert
        Assert.That(nextCalled, Is.True);
        Assert.That(context.Result, Is.Null);
        #endregion
    }

    [Test]
    public async Task OnActionExecutionAsync_MissingSubClaim_Returns401()
    {
        #region Arrange
        var context = BuildContext(sub: null);
        #endregion

        #region Act
        await _filter.OnActionExecutionAsync(context, NextDelegate());
        #endregion

        #region Assert
        Assert.That(context.Result, Is.InstanceOf<UnauthorizedResult>());
        #endregion
    }

    [Test]
    public async Task OnActionExecutionAsync_UnknownSub_Returns401()
    {
        #region Arrange
        _repoMock
            .Setup(r => r.GetByAuth0SubAsync("auth0|unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var context = BuildContext("auth0|unknown");
        #endregion

        #region Act
        await _filter.OnActionExecutionAsync(context, NextDelegate());
        #endregion

        #region Assert
        Assert.That(context.Result, Is.InstanceOf<UnauthorizedResult>());
        #endregion
    }

    [Test]
    public async Task OnActionExecutionAsync_RouteUserIdMatchesToken_CallsNext()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "u@e.com", Auth0Sub = "auth0|abc" };
        _repoMock
            .Setup(r => r.GetByAuth0SubAsync("auth0|abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var context = BuildContext("auth0|abc", routeUserId: userId);
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                [],
                new object()));
        };
        #endregion

        #region Act
        await _filter.OnActionExecutionAsync(context, next);
        #endregion

        #region Assert
        Assert.That(nextCalled, Is.True);
        Assert.That(context.Result, Is.Null);
        Assert.That(context.HttpContext.Items["AuthenticatedUserId"], Is.EqualTo(userId));
        #endregion
    }

    [Test]
    public async Task OnActionExecutionAsync_RouteUserIdMismatch_Returns403()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "u@e.com", Auth0Sub = "auth0|abc" };
        _repoMock
            .Setup(r => r.GetByAuth0SubAsync("auth0|abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var context = BuildContext("auth0|abc", routeUserId: Guid.NewGuid());
        #endregion

        #region Act
        await _filter.OnActionExecutionAsync(context, NextDelegate());
        #endregion

        #region Assert
        Assert.That(context.Result, Is.InstanceOf<ForbidResult>());
        #endregion
    }

    [Test]
    public async Task OnActionExecutionAsync_NoRouteUserId_StoresUserIdAndCallsNext()
    {
        #region Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "u@e.com", Auth0Sub = "auth0|abc" };
        _repoMock
            .Setup(r => r.GetByAuth0SubAsync("auth0|abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var context = BuildContext("auth0|abc");
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                [],
                new object()));
        };
        #endregion

        #region Act
        await _filter.OnActionExecutionAsync(context, next);
        #endregion

        #region Assert
        Assert.That(nextCalled, Is.True);
        Assert.That(context.HttpContext.Items["AuthenticatedUserId"], Is.EqualTo(userId));
        #endregion
    }
}
