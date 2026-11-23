using AgriSyncIndia.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AgriSyncIndia.Controllers;

public sealed record ClerkSessionRequest(string? Token, string? ReturnUrl);

public partial class AccountController
{
    /// <summary>Page that loads Clerk's script (self-hosted glue in wwwroot/js/clerk-callback.js).</summary>
    [HttpGet("ClerkCallback")]
    public IActionResult ClerkCallback(string? returnUrl, bool signOut = false)
    {
        if (!_clerk.Enabled) return NotFound();
        ViewData["Clerk"] = _clerk;
        ViewData["SignOut"] = signOut;
        ViewData["ReturnUrl"] = returnUrl != null && Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        return View();
    }

    /// <summary>Exchanges a Clerk session token for the app cookie. Returns the safe URL to go to.</summary>
    [HttpPost("ClerkSession")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ClerkSession(
        [FromBody] ClerkSessionRequest? body,
        [FromServices] ClerkTokenValidator validator,
        [FromServices] UserProvisioning provisioning)
    {
        if (!_clerk.Enabled) return NotFound();

        var token = await validator.ValidateAsync(body?.Token, HttpContext.RequestAborted);
        if (!token.Succeeded) return Unauthorized(new { error = token.Error });

        var provisioned = await provisioning.ProvisionAsync(token.Identity!, HttpContext.RequestAborted);
        if (!provisioned.Succeeded) return StatusCode(StatusCodes.Status403Forbidden, new { error = provisioned.Error });

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            UserProvisioning.BuildPrincipal(provisioned.User!),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14) });

        var next = !string.IsNullOrEmpty(body?.ReturnUrl) && Url.IsLocalUrl(body.ReturnUrl)
            ? body.ReturnUrl
            : Url.Action("Dashboard", "Climate")!;
        return Ok(new { redirect = next });
    }
}
