using System.Security.Claims;

namespace JobTracker.Api.Extensions;

public static class UserContextExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("The token does not contain a user id.");
    }
}