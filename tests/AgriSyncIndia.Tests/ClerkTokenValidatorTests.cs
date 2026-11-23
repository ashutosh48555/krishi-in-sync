using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AgriSyncIndia.Services;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AgriSyncIndia.Tests;

public class ClerkTokenValidatorTests
{
    const string Issuer = "https://test.clerk.accounts.dev";
    const string Origin = "https://app.example.com";
    static readonly RsaSecurityKey Rsa = new(RSA.Create(2048)) { KeyId = "k1" };

    sealed class FixedKeys(SecurityKey key) : IClerkKeyProvider
    {
        public Task<IReadOnlyCollection<SecurityKey>> GetKeysAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyCollection<SecurityKey>>(new[] { key });
    }

    static ClerkTokenValidator Validator(string frontendApi = Issuer + "/") => new(
        new FixedKeys(Rsa),
        new ClerkOptions { FrontendApi = frontendApi, AllowedOrigin = Origin });

    static string Token(string iss = Issuer, string? azp = Origin, DateTime? exp = null, SecurityKey? key = null,
        string alg = SecurityAlgorithms.RsaSha256)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = "user_1", ["email"] = "a@b.in", ["name"] = "Asha"
        };
        if (azp != null) claims["azp"] = azp;
        var now = DateTime.UtcNow;
        var desc = new SecurityTokenDescriptor
        {
            Issuer = iss,
            Claims = claims,
            NotBefore = now.AddMinutes(-2),
            Expires = exp ?? now.AddMinutes(1),
            SigningCredentials = alg == "none" ? null : new SigningCredentials(key ?? Rsa, alg)
        };
        return new JsonWebTokenHandler().CreateToken(desc);
    }

    [Fact]
    public async Task Valid_token_passes_with_claims()
    {
        var r = await Validator().ValidateAsync(Token());
        Assert.True(r.Succeeded);
        Assert.Equal("user_1", r.Identity!.Sub);
        Assert.Equal("a@b.in", r.Identity.Email);
        Assert.Equal("Asha", r.Identity.Name);
    }

    [Fact] public async Task Expired_fails() =>
        Assert.False((await Validator().ValidateAsync(Token(exp: DateTime.UtcNow.AddSeconds(-30)))).Succeeded);

    [Fact] public async Task Wrong_issuer_fails() =>
        Assert.False((await Validator().ValidateAsync(Token(iss: "https://evil.example.com"))).Succeeded);

    [Fact] public async Task Wrong_azp_fails() =>
        Assert.False((await Validator().ValidateAsync(Token(azp: "https://evil.example.com"))).Succeeded);

    [Fact] public async Task Missing_azp_fails() =>
        Assert.False((await Validator().ValidateAsync(Token(azp: null))).Succeeded);

    [Fact] public async Task Alg_none_fails()
    {
        string B(string s) => Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(s));
        var exp = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds();
        var t = $"{B("{\"alg\":\"none\",\"typ\":\"JWT\"}")}.{B($"{{\"iss\":\"{Issuer}\",\"azp\":\"{Origin}\",\"sub\":\"u\",\"exp\":{exp}}}")}.";
        Assert.False((await Validator().ValidateAsync(t)).Succeeded);
    }

    [Fact] public async Task Hs256_signed_with_public_material_fails()
    {
        var hs = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('x', 64))) { KeyId = "k1" };
        Assert.False((await Validator().ValidateAsync(Token(key: hs, alg: SecurityAlgorithms.HmacSha256))).Succeeded);
    }

    [Fact] public async Task Token_signed_by_other_key_fails() =>
        Assert.False((await Validator().ValidateAsync(
            Token(key: new RsaSecurityKey(RSA.Create(2048)) { KeyId = "k1" }))).Succeeded);

    [Fact] public async Task Garbage_fails() =>
        Assert.False((await Validator().ValidateAsync("not-a-jwt")).Succeeded);
}
