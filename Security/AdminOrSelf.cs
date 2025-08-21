using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BarnCaseAPI.Security;

public sealed class AdminOrSelfRequirement : IAuthorizationRequirement { }

public sealed class AdminOrSelfHandler : AuthorizationHandler<AdminOrSelfRequirement, int>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminOrSelfRequirement _, int targetUserId)
    {
        var user = (ClaimsPrincipal)context.User;
        if (user.IsInRole("Admin") || user.UserId() == targetUserId) context.Succeed(_);
        return Task.CompletedTask;
    }
}