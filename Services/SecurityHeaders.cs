namespace AgriSyncIndia.Services;

public static class SecurityHeaders
{
    /// <summary>
    /// Strict policy: all scripts, styles and fonts are self-hosted and the pages carry no inline script or style
    /// (the VPD marker and charts are positioned from JS through the CSSOM, which CSP allows).
    /// The Clerk origin is added to script, connect, frame and image sources only when Clerk is configured.
    /// The Neon Realtime (wss) and Function (https) origins are added to connect-src only.
    /// </summary>
    public static string ContentSecurityPolicy(string clerkOrigin, params string?[] connectOrigins)
    {
        var clerk = string.IsNullOrEmpty(clerkOrigin) ? "" : " " + clerkOrigin;
        var extra = string.Concat(connectOrigins
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Distinct()
            .Select(o => " " + o));
        return string.Join("; ",
            "default-src 'self'",
            $"script-src 'self'{clerk}",
            "style-src 'self'",
            "font-src 'self' data:",
            $"img-src 'self' data:{clerk} https://img.clerk.com",
            $"connect-src 'self'{clerk}{extra}",
            $"frame-src 'self'{clerk}",
            "object-src 'none'", "worker-src 'self' blob:", "base-uri 'self'", "form-action 'self'", "frame-ancestors 'none'");
    }

    /// <summary>Scheme + host of an absolute URL, or null when it is not absolute.</summary>
    public static string? Origin(string? absoluteUrl)
    {
        if (string.IsNullOrWhiteSpace(absoluteUrl)) return null;
        return Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Authority) : null;
    }
}
