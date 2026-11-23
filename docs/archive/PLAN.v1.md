# PLAN.md: system plan

Branch: `redesign`. Order matters: **make it work (1), then sign-in (2), then look (3, 4), then ship (5)**.
A beautiful page with an empty chart loses marks; a plain page with real data does not.

## 0. Known problems (found on the live site, 23 Nov 2026)
| # | Symptom on live site | Root cause | Phase |
|---|---|---|---|
| 1 | Dashboard 24 h chart empty; Analytics all 0; Automation rules (0); Alerts (0); zone "Current" = "Target" | `Data/DbInitializer.cs` seeds users twice (block at ~line 70 and again at "// 4. Seed Users", ~line 230). `Users.Email` has a unique index, so the final `SaveChangesAsync()` (~line 361) throws and rolls back actuators, rules, 24 h logs, alerts. Zones were saved earlier (line ~145), so next boot hits `if (Zones.Any()) return;` and never reseeds. `Program.cs` catches and only logs. | 1 |
| 2 | Banner says Ambient 24.2 °C / RH 68 %, card says Ambient Temperature 26.8 °C / 76.5 % | Two sources shown under one label (live API vs polyhouse zone) | 1 |
| 3 | "+1.8 °C Offset", "Target: 24.5 °C", "Controlled Range" are typed into the Razor view | Hardcoded in `Views/Climate/Dashboard.cshtml` | 1 |
| 4 | Home and Privacy are the default ASP.NET template | Never replaced | 3 |
| 5 | Chart.js, icons, fonts come from CDNs | `_Layout.cshtml` | 3 |
| 6 | One chart with two y axes; Analytics chart mixes four units | Chart config | 4 |
| 7 | First request after idle shows Render's "Service waking up" for about 35 s | Free plan spin-down | 5 |
| 8 | Layout: 6 long nav labels wrap onto two lines at 1470px, overloaded dashboard header | `_Layout.cshtml`, `Dashboard.cshtml` | 3 |

## Phase 0. Preflight
- [ ] `git checkout -b redesign`, `dotnet --version` is 10.x, `dotnet build` is green on a clean clone.
- [ ] Run CLAUDE.md Setup (Impeccable, UI/UX Pro Max). Confirm both skills load.
- [ ] Copy `PRODUCT.md`, `DESIGN.md` to repo root. Create `docs/` for evidence and screenshots.
- [ ] Install Playwright for screenshots: `npm i -D @playwright/test && npx playwright install chromium` (in a `tools/` folder, not the .NET project).
**Check:** app runs locally (`dotnet run`) and the dashboard opens.

## Phase 1. Make the data real (no styling yet)
Files: `Data/DbInitializer.cs`, `Program.cs`, `Controllers/ClimateController.cs`, `ViewModels/DashboardViewModel.cs`, `Views/Climate/Dashboard.cshtml`, new `Services/IstClock.cs`.

1. **Delete the second user block** (`// 4. Seed Users` through `context.Users.AddRange(users);`). The first block already creates both users with valid password hashes.
2. **Make seeding idempotent per section** instead of "Zones exist, so stop": seed zones, actuators, rules, logs, alerts each behind their own `if (!await context.X.AnyAsync())`, saving after each. Wrap the whole thing in one try/catch that logs the full exception and rethrows.
3. **Fail loudly:** in `Program.cs` remove the swallow. If seeding throws, the process must exit so Render shows a failed deploy instead of an empty site.
4. **Add `/healthz`** returning JSON `{ zones, sensorLogs24h, rules, alerts, users, latestLogIst }` and HTTP 503 if `sensorLogs24h == 0`. Use it as Render's `healthCheckPath` (replaces `/Climate/Dashboard`).
5. **`Services/IstClock.cs`**: `static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow.AddHours(5.5), DateTimeKind.Unspecified);` Replace every `DateTime.UtcNow.AddHours(5.5)` in the repo with it (needed if PostgreSQL is ever used, harmless on SQLite).
6. **Reconcile numbers:** find where `PrimaryTemp`, `PrimaryHumidity`, `WeatherBannerText` are built in `ClimateController`. Rule: cards for ambient use the live Open-Meteo reading; zone rows use each zone's latest `SensorLog`. If a zone has no log in the last 3 h, show "No reading yet".
7. **Remove hardcoded text** in `Dashboard.cshtml` ("Target: 24.5°C", "+1.8°C Offset", "Polyhouse Target: 75%", "Controlled Range"). Add to the view model: `Delta = Temp - TargetTemp` per zone and a `Status` enum (`OnTarget`, `Above`, `Below`, `Dry`, `Humid`, `NoReading`) computed in the controller, with thresholds as named constants (temp ±1.5 °C, VPD from README table).
8. **Add `Headline`** (string) to `DashboardViewModel`: e.g. "Polyhouse is 2.3 °C above target. Open field and drip sector are on target." Built from the zone statuses; handles all-normal and no-data cases. Localise for `en`, `hi`, `pa` (reuse existing language switch).
9. Sensor logs for Analytics: the seeded 25 hours are not enough for a "last 2 days" default. Seed **7 days hourly** (168 points × 3 zones) so History is never empty, and have the ingest job append a new row set every refresh.
10. Open-Meteo failure path: timeout 5 s, one retry, then show the last stored reading with its timestamp and a notice. Never throw to the page.

**Check (evidence in `docs/EVIDENCE.md`):**
- [ ] Fresh DB (`rm *.db && dotnet run`): `curl localhost:PORT/healthz` shows non-zero for every count.
- [ ] Dashboard: chart has 24+ points per series, zone "Now" differs from "Target" where data differs, no hardcoded strings (`grep -n "24.5\|+1.8\|Controlled Range" Views/` returns nothing).
- [ ] History: summary figures are non-zero; Automation lists the seeded rules; Alerts lists seeded alerts.
- [ ] Restart the app twice. Data identical, no duplicate rows.

## Phase 2. Sign-in
Follow `AUTH.md`. Public pages stay public; anything that changes data requires sign-in.
**Check:** AUTH.md section 6 checklist.

## Phase 3. Design system and pages
Order: tokens, layout, Home, Dashboard, then the rest. Commit after each.
1. **Assets** (`wwwroot/lib`): self-host Chart.js 4, Bootstrap Icons, Martel, Mukta, Mukta Mahee (see DESIGN.md section 4). Remove all `cdn.jsdelivr.net`, `fonts.googleapis.com`, `fonts.gstatic.com` references.
2. **`wwwroot/css/tokens.css`**: the `:root` block from DESIGN.md section 3 plus type scale and spacing variables.
3. **`wwwroot/css/site.css`**: base, nav, ledger, status, buttons, inputs, panels. **Move everything out of the `<style>` block in `_Layout.cshtml`.** Keep Bootstrap only for the grid/utilities and the collapse/dropdown JS; override its look with the tokens. Watch specificity: avoid two selectors that set the same margin/padding on one element.
4. **`_Layout.cshtml`**: new nav (DESIGN.md section 5), skip link, `lang` from the selected language, footer reduced to one line (project name, data sources, link to Privacy). Drop the tricolor stripe, IST spinner and tech-stack badges.
5. **Home**: implement the wireframe with the live reading (new `HomeController.Index` calls the same weather service; fall back to the last stored reading). Make `/` route to Home; the dashboard is at `/Climate/Dashboard`. Replace Privacy with a real short policy (what is stored: email and name from sign-in; no tracking; Open-Meteo is the only third party).
6. **Dashboard**: header row, headline sentence, zone ledger with VPD band partial (`Views/Shared/_VpdBand.cshtml`: server-rendered SVG, marker position = `(vpd / 2.0)` clamped, limits at 0.4/0.8/1.25/1.6), chart panels, alerts rail. One-time marker animation.
7. **Zones, History, Automation, Clusters, Alerts, Account pages**: per DESIGN.md "Other pages". Replace card grids with tables/definition lists. Rewrite copy per PRODUCT.md voice. Empty states everywhere a list can be empty.
8. **Responsive pass** at 1440, 1024, 768, 390, 320.

**Check:** `/impeccable audit` and `/impeccable critique` on every page, fix all high findings. Screenshots of every page at 1440 and 390 saved to `docs/screens/`, each one looked at and described in EVIDENCE.md in one line.

## Phase 4. Charts
- [ ] Dashboard: two stacked charts (Temperature by zone; VPD with optimal band). Direct end-labels plugin (small inline Chart.js plugin, no dependency). Legend on. Tooltip shows all series.
- [ ] "View data as table" toggle under each chart, `<table>` with `<th scope>`.
- [ ] History: four small charts (temperature, humidity, soil moisture, VPD) sharing the date range. No dual axes.
- [ ] `aria-label` on each canvas states range and latest values.
- [ ] Run `node validate_palette.js "#2a78d6,#eb6834,#1baf7a" --mode light` again if any series colour changes (it must PASS; contrast WARN on aqua is expected and covered by labels and table).
**Check:** charts render with JS errors = 0 (Playwright console listener), tooltips work by mouse and by keyboard focus on the table fallback.

## Phase 5. Quality gate and deploy
1. [ ] `dotnet build -warnaserror` is clean; `dotnet test` if tests exist. Add 3 xUnit tests: VPD formula (README values), status thresholds, headline builder.
2. [ ] axe-core via Playwright on every page: 0 serious/critical. Lighthouse (mobile) accessibility >= 95, performance >= 80.
3. [ ] Network check: Playwright asserts no request leaves the origin except `api.open-meteo.com` (server-side) and the auth provider redirect.
4. [ ] Keyboard-only walk of the 60-second path (PRODUCT.md) works.
5. [ ] Hindi and Punjabi: every page renders with correct fonts, no tofu boxes, no clipped text.
6. [ ] Merge `redesign` into `main`, deploy per `DEPLOY.md`, run its smoke tests against the live URL.
7. [ ] README rewrite: what it is, live URL, demo login, screenshots, architecture sketch, honest note on simulated zones and actuators, how to run locally.
8. [ ] Record a 90-second screen capture of the 60-second path as a fallback for cold-start or network trouble.

## 6. Final checklist
- [ ] Phase 1 to 5 checks all have evidence in `docs/EVIDENCE.md`
- [ ] Live `/healthz` is 200 with non-zero counts
- [ ] Sign-in works with the provider and with the demo account
- [ ] Uptime pinger configured, cold start no longer visible to an evaluator
- [ ] No secrets in git history (`git log -p | grep -i "client_secret\|password"` shows only the documented demo password)
- [ ] README and screenshots updated

## 7. INT402 rubric (paste here, then map each criterion to a page or file)
_empty. Ask the user._
