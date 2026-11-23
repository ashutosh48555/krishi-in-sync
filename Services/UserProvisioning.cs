using System.Security.Claims;
using System.Text.Json;
using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Services;

public sealed record ProvisionResult(User? User, string? Error)
{
    public bool Succeeded => User != null;
}

/// <summary>Maps a verified Clerk identity to a local User row (find, link, or insert).</summary>
public sealed class UserProvisioning(AgriSyncDbContext db, HttpClient http, ClerkOptions options)
{
    public async Task<ProvisionResult> ProvisionAsync(ClerkIdentity id, CancellationToken ct = default)
    {
        var byId = await db.Users.FirstOrDefaultAsync(u => u.ClerkUserId == id.Sub, ct);
        if (byId != null) return new(byId, null);

        var (email, name) = (id.Email?.Trim().ToLowerInvariant(), id.Name?.Trim());
        if (string.IsNullOrEmpty(email) && !string.IsNullOrWhiteSpace(options.SecretKey))
            (email, name) = await FetchProfileAsync(id.Sub, name, ct);
        if (string.IsNullOrEmpty(email))
            return new(null, "Your Clerk account has no email address. Add one in Clerk and sign in again.");

        var byEmail = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);
        if (byEmail != null)
        {
            // Never re-point an account that already belongs to a different Clerk user.
            if (byEmail.ClerkUserId != null) return new(null, "This email is already linked to another sign-in.");
            byEmail.ClerkUserId = id.Sub;
            await db.SaveChangesAsync(ct);
            return new(byEmail, null);
        }

        var user = new User
        {
            FullName = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : Trunc(name, 100),
            Email = email,
            Role = "Agronomist",
            PasswordHash = "",
            ClerkUserId = id.Sub,
            CreatedAt = IstClock.Now
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return new(user, null);
    }

    public static ClaimsPrincipal BuildPrincipal(User user) => new(new ClaimsIdentity(new Claim[]
    {
        new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new(ClaimTypes.Name, user.FullName),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Role, user.Role),
        new("AffiliatedCluster", user.AffiliatedFarmCluster ?? "All Farm Clusters")
    }, CookieAuthenticationDefaults.AuthenticationScheme));

    private static bool IsVerified(JsonElement e) =>
        e.TryGetProperty("verification", out var v) && v.ValueKind == JsonValueKind.Object &&
        v.TryGetProperty("status", out var s) && s.GetString() == "verified";

    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..n];

    // Backend API fallback; the secret is only ever sent as a Bearer header, never logged.
    private async Task<(string? Email, string? Name)> FetchProfileAsync(string sub, string? name, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://api.clerk.com/v1/users/{Uri.EscapeDataString(sub)}");
            req.Headers.Authorization = new("Bearer", options.SecretKey);
            using var res = await http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return (null, name);
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            var root = doc.RootElement;
            var primary = root.TryGetProperty("primary_email_address_id", out var p) ? p.GetString() : null;
            string? email = null;
            if (root.TryGetProperty("email_addresses", out var list))
                foreach (var e in list.EnumerateArray())
                    if (e.GetProperty("id").GetString() == primary && IsVerified(e)) email = e.GetProperty("email_address").GetString();
            string Get(string k) => root.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
            var full = $"{Get("first_name")} {Get("last_name")}".Trim();
            return (email?.Trim().ToLowerInvariant(), string.IsNullOrEmpty(full) ? name : full);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException or TaskCanceledException)
        {
            return (null, name);
        }
    }
}
