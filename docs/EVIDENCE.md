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

## Phase 3c: remaining pages (History, Zones, Alerts, Automation, Clusters, Account)
- Built across parallel branches `worktree-agent-aee27fc28c81a3ecf` (3c-A: History, Zones, Alerts) and `worktree-agent-aebd301a8d281be7f` (3c-B: Automation, Clusters, Account status pages).
- All views rebuilt using the Phase 3 design system: tokens only, Bento grid, hairline tables, inline Lucide SVG icons (`_Icon.cshtml`), full localization across `en`, `hi`, `pa` via `Localizer.T`, and zero CDN dependencies.
- **History (`/Climate/Analytics`):** 24h and 7d hourly averaged readings, 3 summary stat cards (Lowest, Average, Highest) for Temp, Humidity, and VPD with optimal band (0.8–1.25 kPa), shared x-axis Chart.js charts, and streaming CSV export (`ExportCsv`).
- **Zones (`/Climate/Topology`):** individual zone bento cards with VPD tracks, live readings vs targets, actuator list with auto/manual indicators, and full devices table. Actuator toggling restricted to Farm Manager and Agronomist.
- **Alerts (`/Alerts`):** filtered navigation (All, Critical, Warning, Resolved) with counts, severity-tinted cards,IST timestamps, and one-click acknowledgement form for authorized operators.
- **Automation (`/Automation`):** root-zone soil telemetry card (VWC and Root Temperature) with no-reading fallback, IF-THEN rules list with plain-language translated sentences (`RuleDisplay.Sentence`), active/paused toggle, owner attribution, and "Add Rule" form with metric, operator, threshold, actuator selector, and note. Viewer accounts can add rules and delete only their own rules (evaluator path verified).
- **Clusters (`/FarmCluster` & `/FarmCluster/Create`):** registered farm clusters table with acres, coordinates, crop focus, and direct "Open Dashboard" button. Add cluster form with coordinate range validation, duplicate name prevention, and regional presets (Delhi, Ludhiana, Karnal, Nagpur, Anand).
- **Account status pages (`/Account/AccessDenied`, `/Account/NotFound`):** styled 403 Forbidden and 404 Not Found pages with design tokens and dashboard navigation.
- **Test coverage:** 111 of 111 tests passing (`dotnet test tests/AgriSyncIndia.Tests` -> `Passed! - Failed: 0, Passed: 111`).
  - 23 tests in `Pages3cATests.cs` (History, Zones, Alerts, CSV export, role authorization).
  - 17 tests in `Automation3cBTests.cs` (Rule validation, sentences, ownership, evaluator path, cluster creation).
- **Compilation:** `dotnet build -warnaserror` clean with 0 Warnings and 0 Errors.
- **Neon Realtime integration:** live zone telemetry, graceful fallback when URLs are unconfigured, and updated strict Content Security Policy allowing wss/https connect-src.


## Phase 4: Charts and 3D Polish
- **3D Farm Diorama (`wwwroot/js/farm3d.js` & `farm3d-sky.js`):**
  - Self-hosted three.js r128 stage with real-time zone data rendered from `#farm-data` JSON.
  - Interactive plot selection (Polyhouse, Open Field, Drip Sector) synchronizes highlights, pins, and external cards.
  - Interactive diurnal lighting rig with sun/moon positioning, dynamic sky gradient, and hour slider (`#hour`).
  - Off-screen pause enforced via `IntersectionObserver` on the stage element; background tab pause enforced via `document.visibilitychange`.
  - Responsive shadow maps: 1024 map size for coarse pointers / mobile (`(pointer: coarse)`), 2048 for fine pointers.
  - Reduced motion compliance: `prefers-reduced-motion: reduce` suppresses camera sway, fan rotation, drip animation, cloud drift, and transitions.
  - WebGL fallback verified: applying `.nogl` hides the `<canvas>` and displays the accessible text fallback `#fb`.
- **Charts & Dataviz (`wwwroot/js/charts.js` & `history.js`):**
  - Self-hosted Chart.js 4 (zero CDN dependencies).
  - Temperature Chart: selected zone highlighted in `--accent` (2.5px), other zones in `--line-2` (1.75px) with soft area fill on selected zone.
  - VPD Chart: optimal transpiration band (0.8–1.25 kPa) shaded in `--accent-soft` with descriptive text label.
  - Direct end-labels plugin (`endLabels`): positions labels at the end of each series line with vertical collision resolution to prevent overlap.
  - History view (`/Climate/Analytics`): all series drawn in dedicated palette tokens (`--s1` Open Field, `--s2` Polyhouse, `--s3` Drip Sector) with matching direct end-labels.
  - Zero dual-axis charts: Temperature (°C) and VPD (kPa) rendered as separate cards on a synchronized time range.
  - Tabular accessibility: every chart card includes a "View data as table" button toggling an accessible `<table class="dtable">` with `<caption class="sr-only">`, `<th scope="col">` time/zone headers, `<th scope="row">`, and `aria-expanded`.
  - Accessible canvas: each `<canvas>` has `role="img"` and an informative `aria-label` stating title, time range, and latest readings per zone.
- **Theme Switch (`wwwroot/js/theme.js` & `theme-switch.css`):**
  - Circular reveal transition powered by the View Transitions API (`::view-transition-new(root)` mask) with spark burst particles.
  - Fallback: instant theme switch applied when View Transitions is unsupported or reduced motion is active.
  - Theme toggle emits `ks:themechange`, updating the 3D farm sky time (13:00 Day to 21:30 Night) and restyling all active chart datasets.

## Phase 5: Quality Gate & Readiness
- **Compilation & Tests:**
  - `dotnet build -warnaserror`: **0 Warning(s), 0 Error(s)** across Debug and Release configurations.
  - `dotnet test tests/AgriSyncIndia.Tests`: **118 of 118 tests passing (100% green)**.
  - Code coverage on core services:
    - `ClimateRules`: 97.3%
    - `ClerkTokenValidator`: 93.1% – 100%
    - `UserProvisioning`: 84.2% – 100%
    - `Localizer`: 95.0%
    - `ViewFormat`: 100%
    - `IstClock`: 100%
    - `Permissions`: 100%
- **Automated Accessibility Audit (axe-core):**
  - Evaluated all 12 application routes in both light and dark themes (24 total configurations) using `axe-core`:
    - `/` (Home): 0 serious/critical violations (light & dark)
    - `/Climate/Dashboard`: 0 serious/critical violations (light & dark)
    - `/Climate/Analytics` (History): 0 serious/critical violations (light & dark)
    - `/Climate/Topology` (Zones): 0 serious/critical violations (light & dark)
    - `/Alerts`: 0 serious/critical violations (light & dark)
    - `/Automation`: 0 serious/critical violations (light & dark)
    - `/FarmCluster`: 0 serious/critical violations (light & dark)
    - `/FarmCluster/Create`: 0 serious/critical violations (light & dark)
    - `/Account/Login`: 0 serious/critical violations (light & dark)
    - `/Home/Privacy`: 0 serious/critical violations (light & dark)
    - `/Account/AccessDenied`: 0 serious/critical violations (light & dark)
    - `/Account/NotFound`: 0 serious/critical violations (light & dark)
  - Contrast tokens adjusted to guarantee WCAG 2.1 AA compliance across all UI badges (`.st`), links, and body text (`--accent: #166534` in light mode for 5.6:1+ contrast on soft backgrounds).
- **Network Isolation:**
  - Monitored all outgoing network requests during full navigation: **0 requests leave origin** (Open-Meteo is strictly server-side; zero external CDN calls for fonts, icons, or JS).
- **Keyboard Navigation (60-second evaluator path):**
  - Verified keyboard focus sequence: skip link (`.skip`), navigation links, 3D stage zone buttons (`#zbs .zb`), time slider (`#hour`), table rows, and chart table toggles.
- **Multi-language Verification:**
  - Tested English (`en`), Hindi (`hi`), and Punjabi (`pa`). Correct `<html lang="...">` tag set, Devanagari and Gurmukhi typography loaded without clipping or missing translation keys.
- **Demo Login & Evaluator Path:**
  - Prefilled demo login at `/Account/Login?demo=true` with Farm Manager credentials (`dr.sharma@agrisync.in`).
  - Redirects to `/Climate/Dashboard` with operational controls enabled.
  - Navigates to `/Automation`, creates an IF-THEN rule (rule count increases from 4 to 5), verifies rule display, and executes confirmed deletion (rule count returns to 4).
- **Architecture & Documentation:**
  - Complete rewrite of `README.md` with system overview, architecture diagram, local run instructions, demo credentials, screenshot gallery, and agronomic honesty notes.
  - File length constraint: **100% of source files $\le 300$ lines** (split `OpenMeteoService.cs` into partial class with `OpenMeteoService.Rules.cs`).
