using System.Security.Claims;
using AgriSyncIndia.Models;

namespace AgriSyncIndia.Services;

/// <summary>Who may change what. Operators are the seeded demo roles; everyone else is a Viewer.</summary>
public static class Permissions
{
    public static bool IsOperator(ClaimsPrincipal user) => user.IsInRole("Farm Manager") || user.IsInRole("Agronomist");

    public static int? UserId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static bool CanModify(ClaimsPrincipal user, AutomationRule rule) =>
        IsOperator(user) || (rule.OwnerUserId != null && rule.OwnerUserId == UserId(user));
}
