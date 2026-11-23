using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;

namespace AgriSyncIndia.Services;

/// <summary>
/// UI strings for en, hi, pa. The language comes from <c>?lang=</c> (the original mechanism), is remembered in the
/// <c>ks-lang</c> cookie and is placed in <c>HttpContext.Items["lang"]</c> by <see cref="LanguageMiddleware"/>.
/// Views use <c>@Context.T("key")</c>; add new keys to <see cref="Strings"/>.
/// </summary>
public static class Localizer
{
    public static readonly string[] Languages = { "en", "hi", "pa" };

    public static string? Normalize(string? lang) => lang?.ToLowerInvariant() switch
    {
        "en" => "en", "hi" => "hi", "pa" => "pa", _ => null
    };

    public static string Lang(this HttpContext ctx) => ctx.Items["lang"] as string ?? "en";

    public static string T(string lang, string key)
    {
        if (!Strings.All.TryGetValue(key, out var v)) return key; // visible when a key is missing
        return lang switch { "hi" => v.Hi, "pa" => v.Pa, _ => v.En };
    }

    public static string T(this HttpContext ctx, string key) => T(ctx.Lang(), key);

    public static string T(this HttpContext ctx, string key, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, T(ctx.Lang(), key), args);

    /// <summary>Same path and query with <c>lang</c> replaced (used by the language control).</summary>
    public static string LangUrl(this HttpContext ctx, string lang)
    {
        var q = QueryHelpers.ParseQuery(ctx.Request.QueryString.Value)
            .Where(p => !p.Key.Equals("lang", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(p => p.Key, p => p.Value.ToString());
        q["lang"] = lang;
        return QueryHelpers.AddQueryString(ctx.Request.Path.Value ?? "/", q!);
    }

    /// <summary>Inline Lucide icon from the _Icon partial, e.g. <c>@await Html.Icon("bell", 16)</c>.</summary>
    public static Task<IHtmlContent> Icon(this IHtmlHelper html, string name, int size = 16) =>
        html.PartialAsync("_Icon", $"{name}:{size}");
}

/// <summary>Reads <c>?lang=</c> or the cookie and stores the result for the request.</summary>
public sealed class LanguageMiddleware
{
    private readonly RequestDelegate _next;
    public LanguageMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext ctx)
    {
        var fromQuery = Localizer.Normalize(ctx.Request.Query["lang"]);
        var lang = fromQuery ?? Localizer.Normalize(ctx.Request.Cookies["ks-lang"]) ?? "en";
        ctx.Items["lang"] = lang;
        if (fromQuery != null)
            ctx.Response.Cookies.Append("ks-lang", lang, new CookieOptions
            {
                MaxAge = TimeSpan.FromDays(365), SameSite = SameSiteMode.Lax, HttpOnly = true,
                Secure = ctx.Request.IsHttps, IsEssential = true
            });
        return _next(ctx);
    }
}
