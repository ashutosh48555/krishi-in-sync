# AUTH.md: sign-in with OpenID Connect

The app already has cookie auth (own Register/Login, `Users` table, demo account). We **add** an external provider
through OpenID Connect and **keep** the demo login, so an evaluator can always get in.

## 1. Provider (needs the user)
The user said a free auth service comes with the GitHub Student Developer Pack. The exact provider was not named.
**Ask which one** (Auth0, Okta, Clerk, Supabase Auth, other) and get these three values. Any standards-compliant OIDC provider works with the code below:

| Value | Example (Auth0) | Where it goes |
|---|---|---|
| Authority | `https://YOUR-TENANT.us.auth0.com` | `Auth__Authority` |
| Client ID | from the "Regular Web Application" | `Auth__ClientId` |
| Client secret | same screen | `Auth__ClientSecret` |

In the provider dashboard create a **Regular Web Application** (server-rendered, uses authorization code flow) and set:
- Allowed callback URLs: `https://krishi-in-sync.onrender.com/signin-oidc`, `https://localhost:5001/signin-oidc` (use the real local port)
- Allowed logout URLs: `https://krishi-in-sync.onrender.com/`, `https://localhost:5001/`
- Allowed web origins: the same two origins
- Turn on "Sign up" if the provider has it, so evaluators can create an account themselves.
If the provider is Clerk or Supabase Auth, use its OIDC/OAuth server endpoints as the Authority, or tell the user the setup differs; do not guess.

## 2. Package
```bash
dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect   # match the net10 version
```

## 3. `Program.cs`
Keep the existing cookie scheme and its options. Add the OIDC scheme and forwarded headers **before** auth middleware.

```csharp
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear(); o.KnownProxies.Clear();   // Render's proxy is not on a fixed address
});

var auth = builder.Configuration.GetSection("Auth");
var oidcEnabled = !string.IsNullOrEmpty(auth["Authority"]);

var authBuilder = builder.Services.AddAuthentication(/* existing cookie defaults */)
    .AddCookie(/* existing options */);

if (oidcEnabled)
{
    authBuilder.AddOpenIdConnect("oidc", o =>
    {
        o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        o.Authority = auth["Authority"];
        o.ClientId = auth["ClientId"];
        o.ClientSecret = auth["ClientSecret"];
        o.ResponseType = "code";
        o.UsePkce = true;
        o.CallbackPath = "/signin-oidc";
        o.SignedOutCallbackPath = "/signout-callback-oidc";
        o.Scope.Clear(); o.Scope.Add("openid"); o.Scope.Add("profile"); o.Scope.Add("email");
        o.GetClaimsFromUserInfoEndpoint = true;
        o.SaveTokens = false;
        o.TokenValidationParameters.NameClaimType = "name";
        o.Events.OnTokenValidated = async ctx => await UserProvisioning.EnsureLocalUserAsync(ctx);
    });
}

var app = builder.Build();
app.UseForwardedHeaders();          // first in the pipeline, before UseHttpsRedirection
// ... existing: UseHttpsRedirection, UseStaticFiles, UseRouting, UseAuthentication, UseAuthorization
```
**Why forwarded headers matter:** behind Render's proxy the app sees `http://`, so without this the redirect URI sent to the provider is `http://...` and the provider rejects it ("redirect_uri mismatch"). This is the most common failure.

## 4. Local user provisioning
`Services/UserProvisioning.cs`: on first OIDC login, find `Users` by email; if missing, insert `{ FullName = name claim, Email, Role = "Agronomist", PasswordHash = "" , CreatedAt = IstClock.Now }`. Add the local `Role` and (if present) `AffiliatedCluster` as claims on the principal so `_Layout.cshtml` keeps working (it reads `ClaimTypes.Role`, `ClaimTypes.Email`, `"AffiliatedCluster"`). If the provider does not return an email, reject with a clear message.
Password-login must refuse users whose `PasswordHash` is empty.

## 5. Account controller and views
- `AccountController`: add `GET /Account/External` that returns `Challenge(new AuthenticationProperties { RedirectUri = returnUrl ?? "/" }, "oidc")`. `Logout` (POST, anti-forgery) signs out of the cookie scheme and, when OIDC was used, also `oidc`.
- Logout with Auth0 needs the tenant's RP-initiated logout (`end_session_endpoint`) enabled, otherwise handle `OnRedirectToIdentityProviderForSignOut` and send the provider's logout URL with `returnTo`. Check the provider's docs.
- `Login.cshtml`: primary button "Continue with {provider name}" when `oidcEnabled`, then a divider "or use the demo account", then the existing form. Keep the demo credentials note.
- Authorization: audit controllers. Read pages (Dashboard, Zones, History, Alerts, Clusters list, Home) stay public. Require `[Authorize]` on POST actions and on Create/Edit/Delete pages: cluster create, automation rules, actuator toggles, refresh readings. Unauthenticated users get redirected to Login with `returnUrl`.

## 6. Cookies and keys on Render
The container's data-protection keys are lost on every restart, so users are signed out after a deploy or wake-up. Acceptable for a demo; note it in README. If you want sessions to survive, persist keys to the DB (`AddDataProtection().PersistKeysToDbContext<AgriSyncDbContext>()`), which needs one extra table.
Set cookies `Secure`, `HttpOnly`, `SameSite=Lax`. The OIDC correlation cookie needs `SameSite=None; Secure` on some providers; test.

## 7. Verification checklist
- [ ] Local: Continue with provider, sign in, return to the page I came from, name shows in the header.
- [ ] Live: same flow on `https://krishi-in-sync.onrender.com` (no `redirect_uri mismatch`).
- [ ] Demo login (`dr.sharma@agrisync.in` / `AgriSync@123`) still works.
- [ ] Anonymous POST to a protected action is redirected to Login, not executed.
- [ ] Sign out clears the session on both the app and the provider.
- [ ] `ClientSecret` appears in no file in git. `git grep -i secret` shows config key names only.
- [ ] If the `Auth__Authority` variable is unset, the app still runs (demo login only).
