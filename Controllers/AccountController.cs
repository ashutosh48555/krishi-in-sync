using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using AgriSyncIndia.Services;
using AgriSyncIndia.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgriSyncIndia.Controllers;

[Route("[controller]")]
public partial class AccountController : Controller
{
    private readonly AgriSyncDbContext _context;
    private readonly ILogger<AccountController> _logger;
    private readonly ClerkOptions _clerk;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AccountController(AgriSyncDbContext context, ILogger<AccountController> logger, IOptions<ClerkOptions> clerk)
    {
        _context = context;
        _logger = logger;
        _clerk = clerk.Value;
    }

    [HttpGet("Login")]
    public IActionResult Login(string? returnUrl, bool demo = false)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Dashboard", "Climate");
        }

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ClerkEnabled"] = _clerk.Enabled;
        ViewData["Demo"] = demo;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("Login")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ClerkEnabled"] = _clerk.Enabled;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            // Same message for unknown user and Clerk-only user: no account enumeration.
            ModelState.AddModelError(string.Empty, "Incorrect email or password.");
            return View(model);
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            // Backward-compatibility fallback for demo pre-hashed user
            if (user.PasswordHash.Contains("FakeHash") && (model.Password == "AgriSync@123" || model.Password == "password" || model.Password == "admin123"))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
                await _context.SaveChangesAsync();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Incorrect email or password.");
                return View(model);
            }
        }

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, UserProvisioning.BuildPrincipal(user), authProperties);

        _logger.LogInformation("Password sign-in succeeded for user {UserId}.", user.UserId);
        TempData["SuccessMessage"] = string.Format(Localizer.T(HttpContext.Lang(), "toast.login.welcome"), user.FullName, user.Role);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Dashboard", "Climate");
    }

    /// <summary>Clears the app cookie; when Clerk is on, the callback page then ends the Clerk session too.</summary>
    [HttpPost("Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (_clerk.Enabled) return RedirectToAction(nameof(ClerkCallback), new { signOut = true });
        TempData["SuccessMessage"] = Localizer.T(HttpContext.Lang(), "toast.logout");
        return RedirectToAction(nameof(Login));
    }
}
