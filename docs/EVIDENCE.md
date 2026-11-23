# Evidence

## Phase 1
- `rm -f AgriSyncIndia.db && dotnet run --urls http://127.0.0.1:5188`, `curl /healthz` -> `HTTP 200 {"zones":3,"sensorLogs24h":72,"rules":4,"alerts":3,"users":2,"latestLogIst":"2026-11-23T15:47:58.621443"}` (SQLite; 24 h window = 3 zones x 24 rows).
- Two more restarts: identical JSON including `latestLogIst`, no duplicates. `sqlite3 AgriSyncIndia.db "select count(*) from SensorLogs"` -> `504`, Users -> `2`.
- `grep -rn "24.5\|+1.8\|Controlled Range" Views/` -> no output.
- `dotnet build -warnaserror` -> `0 Warning(s)`.
- `dotnet test tests/AgriSyncIndia.Tests` -> `Passed! - Failed: 0, Passed: 33`. Coverage of `Services/ClimateRules.cs`: line-rate 1 (100%).
- `dotnet tool run dotnet-ef migrations list` (no database) -> `20261123101520_InitialCreate`. `migrations script` contains `ENABLE ROW LEVEL SECURITY` for 9 tables.
- Dashboard (`/`) headline rendered from live data: "Polyhouse Nursery is 9.0 °C above target. Open Field is 4.3 °C above target. Drip Sector is 7.1 °C above target."; advice "Open the polyhouse vents and run the fan until evening."
- Not verified (no Postgres here): `Migrate()` on a real database, RLS via anon REST read, the 503 path of `/healthz`, Open-Meteo failure path under a real outage.

## Phase 2
- `dotnet build -warnaserror` -> `0 Warning(s) 0 Error(s)`. `dotnet test tests/AgriSyncIndia.Tests` -> `Passed! - Failed: 0, Passed: 53` (20 new: validator 9, provisioning 7, authorization reflection 4).
- Run on SQLite, no Clerk config: `GET /Account/Login` -> `200`, demo credentials present, "Continue with Clerk" count `0`.
- Demo login (cookie jar + antiforgery token, dr.sharma@agrisync.in): `302 -> /Climate/Dashboard`; then `GET /FarmCluster/Create` -> `200`.
- Anonymous `GET /FarmCluster/Create` -> `302 -> /Account/Login?ReturnUrl=%2FFarmCluster%2FCreate`.
- Anonymous `POST /Automation/DeleteRule` -> `HTTP/1.1 302 Found`, `Location: .../Account/Login?ReturnUrl=%2FAutomation%2FDeleteRule` (not executed).
- Authenticated `POST /Automation/DeleteRule ruleId=99999` with antiforgery -> `404`. Authenticated POST without antiforgery -> `400`.
- `/Account/Register` -> `404`; `/Account/ClerkCallback` without Clerk config -> `404`. Logout POST -> `302 -> /Account/Login`.
- Header present: `Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; ...` (no Clerk origin when unconfigured).
- With fake `Clerk__FrontendApi=https://demo.clerk.accounts.dev`, `PublishableKey=pk_test_FAKE`: Login shows "Continue with Clerk" then "or use the demo account"; CSP `script-src` gains `https://demo.clerk.accounts.dev`; callback page renders Clerk script tags and `/js/clerk-callback.js`; `POST /Account/ClerkSession` with a junk token -> `401 {"error":"Could not load Clerk signing keys."}`; without antiforgery -> `400`.
- `git grep -nI "sk_\|clerk.*secret"` -> only AUTH.md and PLAN.md prose (key names, `sk_...` placeholder). No values in files.
- NOT verified (needs real Clerk keys): the live sign-in, `Clerk.redirectToSignIn` option names, real token `iss`/`azp` values, JWKS fetch over the network, Clerk script CSP needs (telemetry, img hosts), `Clerk.signOut` redirect, Backend API email fallback.
- Known gap: CSP still allows `'unsafe-inline'`, jsDelivr and Google Fonts because the current layout depends on them; Phase 3 removes them.

## Phase 2 fixes
- Verified-email linking: unverified email on a seeded address is refused with "This email belongs to an existing account..." and the row keeps `ClerkUserId` null; verified (token `email_verified` or Backend API `verification.status=verified`) links; new email inserts as Viewer.
- Ownership: `AutomationRule.OwnerUserId` (migration `AddRuleOwner`); non-owner Viewer gets 404 on delete/toggle, owner succeeds, Farm Manager / Agronomist succeed; actuator toggle and alert acknowledge need an operator role.
- `dotnet build -warnaserror` -> 0 warnings; `dotnet test` -> `Passed: 65`. Fresh SQLite run: `/Automation` 200, `/healthz` 200.
- Not verified: migration applied to a real Postgres; `email_verified` claim name in a real Clerk token; browser flow as a Clerk Viewer.

## Phase 3a: design foundation (assets, tokens, theme, 3D module, icons)
- Static smoke page `wwwroot/dev/foundation.html` (git-ignored) served with `python3 -m http.server`, driven by Playwright Chromium (swiftshader WebGL).
- Screenshots (looked at, match the reference): `docs/screens/foundation-{1440,390}-{light,dark}.png`, plus `-full` and `-toggled` variants. Light: blue sky, sun disc, rays, clouds, three plots, pins, glass bar. Dark: stars, moon, night grade, polyhouse lamp glow.
- Console: 0 errors. Requests to non-localhost origins: 0 (asserted in script).
- Theme toggle: light to dark flips `data-theme`, `ks-theme` in localStorage, stage hour 13.00 to 21.50; dark to light returns to 13.00. Initial hour is 13 (light) or 21.5 (dark).
- Not verified: mid-animation frames of the circular reveal, Safari/Firefox fallback crossfade, `_Icon.cshtml` compile (not built to avoid touching the tree during Phase 2), real touch device.

## Phase 3b: layout, Home, Dashboard, sign-in, CSP
- Built on branch `p3b` in a separate worktree (another session was using the main tree and stashed this work); `dotnet build -warnaserror` -> 0 Warning(s), 0 Error(s); `dotnet test` -> `Passed: 68` (3 new CSP tests).
- Fresh SQLite run (no db file), Playwright Chromium (swiftshader). Screenshots looked at: `docs/screens/p3b-{home,dash,login}-{1440,390}-{light,dark}[-full].png`, `p3b-home-1440-toggled-dark.png`. Home matches the reference (two-column hero, lit 3D stage with bar, bento, VPD tracks, direct end labels); Dashboard has header row, headline and advice, zones table, stacked temperature and VPD charts (band shaded), alerts beside them; sign-in is a centred 400px column.
- Widths 1440, 1024, 768, 390, 320, both themes: no horizontal overflow (checked scrollWidth). Console errors 0, `securitypolicyviolation` events 0, requests to non-localhost origins 0. Only console warnings are swiftshader "GPU stall" messages.
- CSP: `script-src 'self'; style-src 'self'; font-src 'self' data:` (no unsafe-inline, no jsDelivr, no Google Fonts). Pages have no inline script or style (VPD marker and charts are positioned from JS through the CSSOM).
- axe-core (bypassing CSP only to inject it) on `/`, `/Climate/Dashboard`, `/Account/Login`, `/Home/Privacy`, 1440 and 390, light and dark: 0 serious or critical after token fixes (`--text-3` light #64655d, dark #8f9288; light `--watch` #92400e).
- Interaction: zone selection by bar button, table row click, keyboard (Enter and Space on the zone button) and 3D click each update the Selected zone card, table, chart highlight and 3D together. Theme toggle: `data-theme` flips, stage time 13:00 -> 21:30 Night. `?lang=hi` sets `<html lang="hi">`, Hindi headline, cookie keeps it on the next page.
- Demo login: `/Account/Login?demo=true` prefills; POST -> 302 `/Climate/Dashboard`; Dashboard then shows "Refresh readings".
- `grep -rnE "cdn\.|googleapis|jsdelivr" Views wwwroot` (outside wwwroot/lib, wwwroot/dev) -> no output.
- Removed: Bootstrap CSS and JS, Bootstrap Icons, Google Fonts, jQuery from the layout (jQuery plus validation load only through `_ValidationScriptsPartial`, used by FarmCluster/Create), old inline styles/scripts, IST clock, tricolor stripe, tech badges, `_Layout.cshtml.css`. Views not yet restyled (Alerts, Automation, Analytics, Topology, FarmCluster) still use Bootstrap class names and look unstyled until Phase 3c.
- Not verified: Clerk script CSP needs (style-src may need more when Clerk renders UI), real touch device, Safari/Firefox, Lighthouse, Hindi/Punjabi on every width (320/390 overflow checked in English only plus the Hindi home headline).
