using AgriSyncIndia.Services;
using System.Security.Claims;
using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using AgriSyncIndia.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Controllers;

[Route("[controller]")]
public class AccountController : Controller
{
    private readonly AgriSyncDbContext _context;
    private readonly ILogger<AccountController> _logger;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AccountController(AgriSyncDbContext context, ILogger<AccountController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET: /Account/Login
    /// </summary>
    [HttpGet("Login")]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Dashboard", "Climate");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// POST: /Account/Login
    /// </summary>
    [HttpPost("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.Trim().ToLower());
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login credentials. User not found.");
            return View(model);
        }

        // Verify password
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            // Backward-compatibility fallback for demo pre-hashed user
            if (user.PasswordHash.Contains("FakeHash") && (model.Password == "AgriSync@123" || model.Password == "password" || model.Password == "admin123"))
            {
                // Accept and upgrade hash
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
                await _context.SaveChangesAsync();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Incorrect email or password.");
                return View(model);
            }
        }

        // Create authentication claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("AffiliatedCluster", user.AffiliatedFarmCluster ?? "All Farm Clusters")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        _logger.LogInformation("User {Email} signed in successfully.", user.Email);
        TempData["SuccessMessage"] = $"Welcome back, {user.FullName}! Logged in as {user.Role}.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Dashboard", "Climate");
    }

    /// <summary>
    /// GET: /Account/Register (Sign Up)
    /// </summary>
    [HttpGet("Register")]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Dashboard", "Climate");
        }

        ViewBag.Clusters = await _context.FarmClusters.Select(c => c.Name).ToListAsync();
        return View(new RegisterViewModel());
    }

    /// <summary>
    /// POST: /Account/Register (Sign Up)
    /// </summary>
    [HttpPost("Register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!model.AgreeToAgrometCompliance)
        {
            ModelState.AddModelError(nameof(model.AgreeToAgrometCompliance), "You must agree to the farm data telemetry protocol.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Clusters = await _context.FarmClusters.Select(c => c.Name).ToListAsync();
            return View(model);
        }

        // Check if email already registered
        bool emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.Trim().ToLower());
        if (emailExists)
        {
            ModelState.AddModelError("Email", "An account with this email address is already registered.");
            ViewBag.Clusters = await _context.FarmClusters.Select(c => c.Name).ToListAsync();
            return View(model);
        }

        var newUser = new User
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            Role = model.Role,
            AffiliatedFarmCluster = model.AffiliatedFarmCluster ?? "IARI Pusa Smart Farm - New Delhi",
            CreatedAt = IstClock.Now
        };

        newUser.PasswordHash = _passwordHasher.HashPassword(newUser, model.Password);

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New user registered: {Email} ({Role})", newUser.Email, newUser.Role);

        // Automatically sign in the newly registered user
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, newUser.UserId.ToString()),
            new(ClaimTypes.Name, newUser.FullName),
            new(ClaimTypes.Email, newUser.Email),
            new(ClaimTypes.Role, newUser.Role),
            new("AffiliatedCluster", newUser.AffiliatedFarmCluster)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        TempData["SuccessMessage"] = $"Registration successful! Welcome to AgriSync India, {newUser.FullName}.";
        return RedirectToAction("Dashboard", "Climate");
    }

    /// <summary>
    /// Sign out / Logout
    /// </summary>
    [HttpPost("Logout")]
    [HttpGet("Logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "You have been logged out safely.";
        return RedirectToAction("Login", "Account");
    }
}
