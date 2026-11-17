using System.Security.Claims;

namespace IronMindGym.Security;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true &&
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0 ? id : null;
}
