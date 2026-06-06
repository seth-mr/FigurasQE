using System.Security.Claims;

namespace MicroservicioFiguras.Helpers;

public static class JwtClaimsHelper
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub") ?? user.FindFirst("userId");
        return idClaim?.Value is string value && int.TryParse(value, out var id) ? id : null;
    }

    public static string? GetUserRole(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value;
    }
}
