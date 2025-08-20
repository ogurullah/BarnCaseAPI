using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BarnCaseAPI.Security;

public sealed class AdminOrOwnerRequirement : IAuthorizationRequirement { }

public sealed class AdminOrOwnerHandler : AuthorizationHandler<AdminOrOwnerRequirement, int>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminOrOwnerRequirement _, int ownerId)
    {
        var user = (ClaimsPrincipal)context.User;
        if (user.IsInRole("Admin") || user.UserId() == ownerId) context.Succeed(_);
        return Task.CompletedTask;
    }
}