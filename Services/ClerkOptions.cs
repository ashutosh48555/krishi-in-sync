namespace AgriSyncIndia.Services;

/// <summary>Bound from the "Clerk" config section. Values come from env vars or user-secrets only.</summary>
public sealed class ClerkOptions
{
    public string? FrontendApi { get; set; }
    public string? PublishableKey { get; set; }
    /// <summary>The app's own origin(s), comma separated. Compared to the token's azp claim.</summary>
    public string? AllowedOrigin { get; set; }
    public string? SecretKey { get; set; }

    public bool Enabled => !string.IsNullOrWhiteSpace(FrontendApi) && !string.IsNullOrWhiteSpace(PublishableKey);

    /// <summary>Frontend API as an https origin with no trailing slash, e.g. https://x.clerk.accounts.dev.</summary>
    public string Issuer
    {
        get
        {
            var v = (FrontendApi ?? "").Trim().TrimEnd('/');
            if (v.Length == 0) return "";
            return v.Contains("://") ? v : "https://" + v;
        }
    }

    public IReadOnlyList<string> AllowedOrigins =>
        (AllowedOrigin ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(o => o.TrimEnd('/')).ToArray();
}
