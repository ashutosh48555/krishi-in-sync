# AUTH.md: sign-in with Clerk (plus the demo account)

Decision (user, 23 Nov 2026): **Clerk hosted sign-in pages + server-side JWT verification.** Clerk has no official ASP.NET MVC SDK, so the app
verifies Clerk's session token itself and then issues the **existing cookie** (`AgriSyncAuthCookie`). The demo login stays so an evaluator can always get in.
Old OpenID Connect plan is archived in `docs/archive/AUTH.v1.md`.
**Verify Clerk details against current Clerk docs (use Context7) before coding. Endpoints and token claims below are from memory of Clerk's model and may have changed.**

## 1. Needs from the user
| Value | Where in Clerk | Goes to |
|---|---|---|
| Frontend API URL (e.g. `https://xxxx.clerk.accounts.dev`) | Dashboard, API keys | `Clerk__FrontendApi` (not secret) |
| Publishable key (`pk_…`) | Dashboard, API keys | `Clerk__PublishableKey` (not secret) |
| Secret key (`sk_…`) | Dashboard, API keys | `Clerk__SecretKey`, **only if** the Backend API fallback in section 4 is used |

Also in the Clerk dashboard: enable email sign-up, add `https://krishi-in-sync.onrender.com` and `https://localhost:<port>` as allowed origins and redirect URLs,
and customise the session token to include the user's name and email (Sessions, Customize session token):
`{ "email": "{{user.primary_email_address}}", "name": "{{user.full_name}}" }`.

## 2. Flow
1. Login page: primary button **Continue with Clerk** goes to Clerk's hosted sign-in (`<FrontendApi>/sign-in?redirect_url=<app>/Account/ClerkCallback`).
2. `GET /Account/ClerkCallback` serves a tiny page that loads Clerk's browser script from the Frontend API, waits for `Clerk.load()`, reads `Clerk.session.getToken()`.
   This one third-party script is the auth provider's own and is the only runtime script outside the origin (allowed exception to the "no CDN" rule, see CLAUDE.md).
3. The page `POST`s the token to `/Account/ClerkSession` with the anti-forgery token. Session tokens live about a minute, so exchange immediately.
4. The server validates the token (section 3), upserts the local user, signs in with the existing cookie scheme, redirects to `returnUrl` (local URLs only).
5. Logout (POST, anti-forgery): sign out of the cookie scheme, then send the browser to the callback page with `Clerk.signOut()`.

## 3. Server-side validation (`Services/ClerkTokenValidator.cs`)
Use `Microsoft.IdentityModel.Protocols.OpenIdConnect` `ConfigurationManager` to cache keys from `<FrontendApi>/.well-known/jwks.json`.
`TokenValidationParameters`: `ValidIssuer = FrontendApi` (no trailing slash), `ValidateAudience = false` (Clerk session tokens carry no `aud`), `ValidateLifetime = true`,
`ClockSkew = 5 s`, signing keys from JWKS, algorithms `RS256` only. Then check the `azp` claim equals the app's own origin (from config, not from the request) and reject if absent.
Return `sub`, `email`, `name`. If `email` is missing, use the fallback in section 4 or reject with a clear message.

## 4. Local user provisioning (`Services/UserProvisioning.cs`)
Add nullable unique `ClerkUserId` to `Users`. On login: find by `ClerkUserId`, else by email (and link it), else insert
`{ FullName = name, Email, Role = "Agronomist", PasswordHash = "", ClerkUserId = sub, CreatedAt = IstClock.Now }`.
Put `ClaimTypes.Role`, `ClaimTypes.Email`, `ClaimTypes.Name` and `"AffiliatedCluster"` on the principal so `_Layout.cshtml` keeps working.
Fallback when the token has no email: `GET https://api.clerk.com/v1/users/{sub}` with `Authorization: Bearer <Clerk__SecretKey>` (server-side only, never logged).
Password login refuses users whose `PasswordHash` is empty. The local Register page is removed (Clerk handles sign-up).

## 5. Authorization
Public: Home, Dashboard, Zones, History, Alerts list, Clusters list. `[Authorize]` on every POST and on create, edit and delete pages: cluster create, automation rules,
actuator toggles, alert acknowledge, refresh readings. Unauthenticated users go to Login with `returnUrl`. Unknown or foreign resource IDs return 404 for everyone.

## 6. Cookies, proxy, keys
- `Program.cs`: `UseForwardedHeaders` first (Render's proxy), cookies `Secure`, `HttpOnly`, `SameSite=Lax`.
- Data-protection keys in Postgres (`PersistKeysToDbContext<AgriSyncDbContext>()`) so sessions survive Render restarts. One extra table in the migration.
- Rate-limit `/Account/ClerkSession` and the demo login (built-in `AddRateLimiter`, fixed window).
- `Content-Security-Policy` allows the Clerk Frontend API origin in `script-src`, `connect-src`, `frame-src`; nothing else third-party in the browser.

## 7. Tests (xUnit, mock Clerk, never call the real service)
`ClerkTokenValidator`: valid token passes; expired fails; wrong issuer fails; wrong `azp` fails; `alg=none` or HS256 fails.
`UserProvisioning`: new user inserted; existing email linked; empty email rejected. Controller: anonymous POST to a protected action redirects to Login.

## 8. Verification checklist
- [ ] Local: Continue with Clerk, sign in, return to the page I came from, name shows in the header.
- [ ] Live on `https://krishi-in-sync.onrender.com`: same flow, no redirect or origin error.
- [ ] Demo login (`dr.sharma@agrisync.in` / `AgriSync@123`) still works.
- [ ] Anonymous POST to a protected action is redirected to Login and not executed.
- [ ] Sign out clears the app session and the Clerk session.
- [ ] `git grep -i "sk_\|secret"` finds config key names only. No Clerk secret in git, logs or page source.
- [ ] If `Clerk__FrontendApi` is unset, the app still runs (demo login only, Clerk button hidden).
