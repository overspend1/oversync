using System.Security.Claims;

namespace OverSync.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (subject is null || !Guid.TryParse(subject, out var userId))
        {
            throw new InvalidOperationException("User claim is missing.");
        }

        return userId;
    }
}
