using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PSA.WebAPI.Extensions;

public static class ControllerUserExtensions
{
    public static int GetUserId(this ControllerBase controller)
        => int.TryParse(controller.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public static bool IsRole(this ControllerBase controller, params string[] allowedRoles)
    {
        var role = controller.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        return allowedRoles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    }
}
