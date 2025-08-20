using System.Security.Claims;

namespace BarnCaseAPI.Security;

public static class ClaimsPrincipalExt
{
    public static int UserId(this ClaimsPrincipal user)
        => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static bool IsAdmin(this ClaimsPrincipal user)
        => user.IsInRole("Admin");
}
