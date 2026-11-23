using System.Reflection;
using AgriSyncIndia.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Xunit;

namespace AgriSyncIndia.Tests;

public class AuthorizationTests
{
    static readonly string[] AnonymousPosts = { "AccountController.Login", "AccountController.ClerkSession", "AccountController.Logout" };

    static IEnumerable<MethodInfo> Actions() =>
        typeof(HomeController).Assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));

    static bool IsPost(MethodInfo m) => m.GetCustomAttributes().OfType<HttpPostAttribute>().Any();
    static string Name(MethodInfo m) => $"{m.DeclaringType!.Name}.{m.Name}";

    [Fact]
    public void Every_post_has_antiforgery()
    {
        var bad = Actions().Where(IsPost).Where(m => m.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() == null).Select(Name);
        Assert.Empty(bad);
    }

    [Fact]
    public void Every_post_except_sign_in_and_out_requires_authorization()
    {
        var bad = Actions().Where(IsPost).Where(m => !AnonymousPosts.Contains(Name(m)))
            .Where(m => m.GetCustomAttribute<AuthorizeAttribute>() == null && m.DeclaringType!.GetCustomAttribute<AuthorizeAttribute>() == null)
            .Select(Name);
        Assert.Empty(bad);
    }

    [Fact]
    public void Create_pages_require_authorization()
    {
        var m = typeof(FarmClusterController).GetMethod("Create", new[] { typeof(string) })!;
        Assert.NotNull(m.GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void Register_action_is_gone() =>
        Assert.Null(typeof(AccountController).GetMethod("Register"));
}
