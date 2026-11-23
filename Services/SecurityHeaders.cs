namespace AgriSyncIndia.Services;

public static class SecurityHeaders
{
    /// <summary>
    /// ponytail: 'unsafe-inline' plus the jsDelivr / Google Fonts origins are here only because the current
    /// layout still uses inline scripts/styles and CDN assets. Phase 3 self-hosts them; then drop those four sources.
    /// </summary>
    public static string ContentSecurityPolicy(string clerkOrigin)
    {
        var clerk = string.IsNullOrEmpty(clerkOrigin) ? "" : " " + clerkOrigin;
        return string.Join("; ",
            "default-src 'self'",
            $"script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net{clerk}",
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com",
            "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com",
            $"img-src 'self' data:{clerk} https://img.clerk.com",
            $"connect-src 'self'{clerk}",
            $"frame-src 'self'{clerk}",
            "object-src 'none'", "worker-src 'self' blob:", "base-uri 'self'", "form-action 'self'", "frame-ancestors 'none'");
    }
}
