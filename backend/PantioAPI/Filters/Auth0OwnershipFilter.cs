using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PantioClassLibrary.Interfaces.Repository;

namespace PantioAPI.Filters;

public class Auth0OwnershipFilter(IUserRepository userRepository) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next();
            return;
        }

        var sub = context.HttpContext.User.FindFirst("sub")?.Value ?? context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (sub is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var user = await userRepository.GetByAuth0SubAsync(sub, context.HttpContext.RequestAborted);
        if (user is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items["AuthenticatedUserId"] = user.Id;

        var today = DateTime.UtcNow.Date;
        if (user.LastActivityAt?.Date != today)
            await userRepository.UpdateLastActivityAsync(user.Id, DateTime.UtcNow, context.HttpContext.RequestAborted);

        if (context.ActionArguments.TryGetValue("userId", out var routeValue) && routeValue is Guid routeUserId && routeUserId != user.Id)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
