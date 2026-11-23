using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AgriSyncIndia.Services;

public sealed record ClerkIdentity(string Sub, string? Email, string? Name, bool EmailVerified = false);

public sealed record ClerkTokenResult(ClerkIdentity? Identity, string? Error)
{
    public bool Succeeded => Identity != null;
}

public interface IClerkKeyProvider
{
    Task<IReadOnlyCollection<SecurityKey>> GetKeysAsync(CancellationToken ct = default);
}

/// <summary>Fetches and caches Clerk's JWKS (cache and refresh handled by ConfigurationManager).</summary>
public sealed class ClerkJwksKeyProvider : IClerkKeyProvider
{
    private sealed class Retriever : IConfigurationRetriever<JsonWebKeySet>
    {
        public async Task<JsonWebKeySet> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel) =>
            new(await retriever.GetDocumentAsync(address, cancel));
    }

    private readonly ConfigurationManager<JsonWebKeySet>? _manager;

    public ClerkJwksKeyProvider(ClerkOptions options)
    {
        if (options.Issuer.Length == 0) return;
        _manager = new ConfigurationManager<JsonWebKeySet>(
            options.Issuer + "/.well-known/jwks.json", new Retriever(), new HttpDocumentRetriever { RequireHttps = true })
        { AutomaticRefreshInterval = TimeSpan.FromHours(1) };
    }

    public async Task<IReadOnlyCollection<SecurityKey>> GetKeysAsync(CancellationToken ct = default)
    {
        if (_manager == null) return Array.Empty<SecurityKey>();
        var set = await _manager.GetConfigurationAsync(ct);
        return set.GetSigningKeys().ToArray();
    }
}

/// <summary>Validates a Clerk session JWT: RS256 only, issuer, lifetime (5 s skew), azp. No audience (Clerk sets none).</summary>
public sealed class ClerkTokenValidator(IClerkKeyProvider keys, ClerkOptions options)
{
    private static ClerkTokenResult Fail(string why) => new(null, why);

    public async Task<ClerkTokenResult> ValidateAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token) || options.Issuer.Length == 0 || options.AllowedOrigins.Count == 0)
            return Fail("Sign-in is not configured or the token is empty.");

        IReadOnlyCollection<SecurityKey> signingKeys;
        try { signingKeys = await keys.GetKeysAsync(ct); }
        catch (Exception) { return Fail("Could not load Clerk signing keys."); }

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromSeconds(5),
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys
        };

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);
        if (!result.IsValid || result.SecurityToken is not JsonWebToken jwt) return Fail("Invalid session token.");

        if (!jwt.TryGetPayloadValue<string>("azp", out var azp) || !options.AllowedOrigins.Contains(azp.TrimEnd('/')))
            return Fail("Session token was not issued for this site.");

        var sub = jwt.Subject;
        if (string.IsNullOrEmpty(sub)) return Fail("Session token has no subject.");

        jwt.TryGetPayloadValue<string>("email", out var email);
        jwt.TryGetPayloadValue<string>("name", out var name);
        // Optional claim (needs adding to the session token template); absent means "not proven".
        var verified = jwt.TryGetPayloadValue<bool>("email_verified", out var ev) && ev;
        return new ClerkTokenResult(new ClerkIdentity(sub, email, name, verified), null);
    }
}
