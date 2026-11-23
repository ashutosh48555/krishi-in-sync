namespace AgriSyncIndia.Services;

public static class SecurityHeaders
{
    /// <summary>
    /// Strict policy: all scripts, styles and fonts are self-hosted and the pages carry no inline script or style
    /// (the VPD marker and charts are positioned from JS through the CSSOM, which CSP allows).
    /// The Clerk origin is added to script, connect, frame and image sources only when Clerk is configured.
    /// </summary>
    public static string ContentSecurityPolicy(string clerkOrigin)
    {
        var clerk = string.IsNullOrEmpty(clerkOrigin) ? "" : " " + clerkOrigin;
        return string.Join("; ",
            "default-src 'self'",
            $"script-src 'self'{clerk}",
            "style-src 'self'",
            "font-src 'self' data:",
            $"img-src 'self' data:{clerk} https://img.clerk.com",
            $"connect-src 'self'{clerk}",
            $"frame-src 'self'{clerk}",
            "object-src 'none'", "worker-src 'self' blob:", "base-uri 'self'", "form-action 'self'", "frame-ancestors 'none'");
    }
}
