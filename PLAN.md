# PLAN.md: system plan (v2)

Branch: `redesign`. Order matters: **make it work (1), sign-in (2), look (3, 4), ship (5)**. A beautiful page with an empty chart loses marks; a plain page with real data does not.
Decisions (user, 23 Nov 2026): Postgres for all data (**Supabase or Neon, provider chosen later, code stays provider-agnostic**), Clerk sign-in plus demo login, new design = `DESIGN.md` v2 and `docs/design-reference/krishi-final-preview.html`.
Project root for code: the folder with `AgriSyncIndia.csproj` (in the zip it is `extracted/MODERN WEB KRISHI/`; in the GitHub repo check where it sits).

## 0. Known problems (found on the live site, 23 Nov 2026)
| # | Symptom on live site | Root cause | Phase |
|---|---|---|---|
| 1 | Dashboard 24 h chart empty; Analytics all 0; Automation rules (0); Alerts (0); zone "Current" = "Target" | `Data/DbInitializer.cs` seeds users twice (~line 70 and "// 4. Seed Users", ~line 230). `Users.Email` is unique, so the final `SaveChangesAsync()` (~line 361) throws and rolls back actuators, rules, 24 h logs, alerts. Zones were saved earlier, so the next boot hits `if (Zones.Any()) return;` and never reseeds. `Program.cs` catches and only logs. | 1 |
| 2 | Banner says Ambient 24.2 °C / 68 %, card says Ambient 26.8 °C / 76.5 % | Two sources under one label (live API vs polyhouse zone) | 1 |
| 3 | "+1.8 °C Offset", "Target: 24.5 °C", "Controlled Range" typed into the Razor view | Hardcoded in `Views/Climate/Dashboard.cshtml` | 1 |
| 4 | Home and Privacy are the default ASP.NET template | Never replaced | 3 |
| 5 | Chart.js, icons, fonts load from CDNs | `_Layout.cshtml` | 3 |
| 6 | One chart with two y axes; Analytics chart mixes four units | Chart config | 4 |
| 7 | First request after idle shows Render's "Service waking up" for about 35 s | Free plan spin-down | 5 |
| 8 | 6 nav labels wrap at 1470px, overloaded dashboard header | `_Layout.cshtml`, `Dashboard.cshtml` | 3 |
| 9 | SQL Server probe in `Program.cs` parses every connection string as SQL Server; old `Migrations/` made for another provider | Provider mix | 1 |

## Phase 0. Preflight
- [x] Cloned `github.com/ashutosh48555/krishi-in-sync` to `Int-402/krishi-in-sync` (identical to `extracted/`), branch `redesign`, `dotnet 10.0.302`, `dotnet build` green (0 errors, 4 warnings). Dockerfile and render.yaml are in the repo.
- [ ] Install design skills (CLAUDE.md Setup). Confirm they load. Playwright in `tools/`, not in the .NET project.
- [ ] `docs/` already holds `design-reference/` and `archive/` (old docs). Add `docs/EVIDENCE.md`.
- [ ] User sets secrets themselves (never pasted into chat): `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<string>"`, `Clerk:FrontendApi`, `Clerk:PublishableKey` (DEPLOY.md, AUTH.md). `gh auth` is broken on this machine (keyring login failed): run `gh auth login` before the push in Phase 5.
**Check:** app runs with `dotnet run`, dashboard opens.

## Phase 1. Real data on Supabase (no styling yet)
Files: `Program.cs`, `Data/DbInitializer.cs`, `Data/AgriSyncDbContext.cs`, `Controllers/ClimateController.cs`, `ViewModels/DashboardViewModel.cs`, `Views/Climate/Dashboard.cshtml`, `Migrations/`, new `Services/IstClock.cs`.
1. **Provider:** add Npgsql, remove SQL Server package and probe. Postgres when a connection string is set (Supabase or Neon, no provider name in code), SQLite otherwise (DEPLOY.md section 1). `Migrate()` uses `ConnectionStrings:Migrations` when present.
2. **Migrations:** delete old, regenerate `InitialCreate` for Postgres, `Migrate()` at startup, RLS on for every table, data-protection keys table, nullable unique `Users.ClerkUserId`.
3. **`IstClock.Now`** (`DateTime.SpecifyKind(DateTime.UtcNow.AddHours(5.5), DateTimeKind.Unspecified)`) replaces every `DateTime.UtcNow.AddHours(5.5)`.
4. **Seeder:** delete the second user block. Seed zones, actuators, rules, logs, alerts each behind `if (!await context.X.AnyAsync())`, saving after each. One try/catch that logs the full exception and **rethrows**.
5. **Fail loudly:** remove the swallow in `Program.cs`. A failed seed must stop the process so Render shows a failed deploy.
6. **`/healthz`:** JSON `{ zones, sensorLogs24h, rules, alerts, users, latestLogIst }`, HTTP 503 if `sensorLogs24h == 0`. It queries the DB, so it also keeps Supabase awake.
7. **Reconcile numbers:** ambient cards use the live Open-Meteo reading; zone rows use each zone's latest `SensorLog`. No log in 3 h shows "No reading yet".
8. **Remove hardcoded text** from `Dashboard.cshtml`. View model gets `Delta = Temp - TargetTemp`, `Status` enum (`OnTarget`, `Above`, `Below`, `Dry`, `Humid`, `NoReading`), VPD band, and thresholds as named constants (temp ±1.5 °C; VPD bands 0.4 / 0.8 / 1.25 / 1.6).
9. **`Headline` and `Advice`** in the view model, built from zone statuses, localised `en`, `hi`, `pa`. Advice rule: polyhouse above target gives "Open the polyhouse vents and run the fan until evening"; below gives "Close the vents to hold heat"; otherwise "No action needed today". Honesty: advice comes from these rules only.
10. **Logs:** seed 7 days hourly for 3 zones (504 rows) so History is never empty; each refresh appends a row set.
11. **Open-Meteo failure path:** 5 s timeout, one retry, then the last stored reading with its timestamp and a notice. Never throw to the page.
**Check (evidence in `docs/EVIDENCE.md`):**
- [ ] Fresh DB: `curl localhost:PORT/healthz` shows non-zero counts. Restart twice: identical counts, no duplicates.
- [ ] Anon-key read against Supabase REST returns no rows (RLS works).
- [ ] `grep -rn "24.5\|+1.8\|Controlled Range" Views/` returns nothing.

## Phase 2. Sign-in (Clerk + demo)
Follow `AUTH.md`. Public pages stay public; anything that changes data requires sign-in.
Use TDD for `ClerkTokenValidator` and `UserProvisioning` (tests first, mocked Clerk).
**Check:** AUTH.md section 8 checklist.

## Phase 3. Design system and pages (needs `DESIGN.md` v2)
Order: assets and tokens, layout and theme, Home with 3D, Dashboard, rest. One commit each.
1. **Assets** in `wwwroot/lib`: Inter Tight, Inter, Noto Sans Devanagari, Noto Sans Gurmukhi (woff2), Chart.js 4, three.js r128, `NOTICE.txt` (Lucide licence). Remove every CDN reference.
2. **`wwwroot/css/tokens.css`** (DESIGN.md section 3), **`site.css`** (base, nav, cards, table, pills, buttons, inputs), **`hero3d.css`** (stage, sky layers, bar, pins). Move the `<style>` block out of `_Layout.cshtml`. Keep Bootstrap only if still needed for JS collapse; otherwise remove it. Each file ≤ 300 lines.
3. **Theme:** inline head script (no flash), `wwwroot/js/theme.js` for the toggle and the switch effect (DESIGN.md section 7), `Views/Shared/_Icon.cshtml` for icons.
4. **`_Layout.cshtml`:** sticky nav, language segment (EN, हिं, ਪੰ), theme button, Sign in pill, skip link, one-line footer. Drop the tricolor stripe, IST spinner and tech badges.
5. **Home:** two-column hero + 3D stage + bento row + zones table + chart + alerts, wired to real view-model data. `wwwroot/js/farm3d.js` reads zone data from a JSON block rendered by Razor (no inline numbers). `/` routes to Home; dashboard stays at `/Climate/Dashboard`. Real Privacy page.
6. **Dashboard:** header, headline, advice, zones table with VPD track (`Views/Shared/_VpdTrack.cshtml`, server-rendered), two charts, alerts.
7. **Zones, History, Automation, Clusters, Alerts, Account:** per DESIGN.md section 5. Tables and bento cards, empty states, copy per PRODUCT.md voice.
8. **Localisation:** `.resx` (or one `Localizer`) for en, hi, pa. No strings inline in views.
9. **Responsive pass** at 1440, 1024, 768, 390, 320, light and dark.
**Check:** `/impeccable audit` and `critique` on every page. Screenshots of every page at 1440 and 390 in **both themes** in `docs/screens/`, each looked at and described in one line in EVIDENCE.md.

## Phase 4. Charts and 3D polish
- [ ] Dashboard: temperature chart (selected zone accent, others grey, direct end-labels) and VPD chart with optimal band. "View data as table" under each. `aria-label` states range and latest values.
- [ ] History: four small charts on a shared x range, `--s1/--s2/--s3` with labels and table. No dual axes.
- [ ] 3D: pause off-screen and when tab hidden, shadow 1024 on touch, WebGL fallback tested by disabling it, reduced motion tested.
- [ ] Theme switch checked in Chrome and in a browser without View Transitions (fade fallback). Tune duration and feather by eye.
**Check:** Playwright console listener reports 0 JS errors on every page; tooltips work by mouse and by keyboard through the table fallback.

## Phase 5. Quality gate and deploy
1. [ ] `dotnet build -warnaserror` is clean. xUnit tests: VPD formula, status thresholds, headline and advice builder, Clerk validator, provisioning. 80 %+ coverage on services.
2. [ ] axe-core via Playwright on every page in both themes: 0 serious or critical. Lighthouse mobile: accessibility ≥ 95, performance ≥ 80 on Home. Fix contrast tokens first.
3. [ ] Network check: no request leaves the origin except Clerk (sign-in only). Open-Meteo is server-side.
4. [ ] Keyboard-only walk of the 60-second path works, including zone buttons and the time slider.
5. [ ] Hindi and Punjabi on every page: correct fonts, no tofu, no clipped text, headline fits.
6. [ ] Merge `redesign` into `main`, deploy per `DEPLOY.md`, run its smoke tests live.
7. [ ] README rewrite: what it is, live URL, demo login, screenshots (light and dark), architecture sketch (Razor, EF Core, Supabase, Clerk, Open-Meteo, three.js), honest note on simulated zones and actuators, local run steps.
8. [ ] 90-second screen capture of the 60-second path, including the theme switch.

## 6. Final checklist
- [ ] Phase 1 to 5 checks all have evidence in `docs/EVIDENCE.md`
- [ ] Live `/healthz` is 200 with non-zero counts, data survives a Render restart
- [ ] Clerk sign-in and the demo account both work live
- [ ] RLS is on for every table; anon REST read returns nothing
- [ ] Uptime pinger configured, cold start not visible to an evaluator
- [ ] No secrets in git history (`git log -p | grep -i "sk_\|password"` shows only the documented demo password)
- [ ] README and screenshots updated

## 7. INT402 rubric
_Not provided by the user (23 Nov 2026). Until it arrives, the success bar is the 60-second evaluator path in PRODUCT.md. Paste the rubric here and map each criterion to a page or file._

## 8. Sub-agent plan (keeps tokens low)
Main thread owns plan, reviews and the preview gate. Phase 1: one general agent. Phase 2: one general agent after Phase 1. Phase 3: tokens, layout and theme by main thread first, then three parallel agents:
(C) Home + Dashboard, (D) History + Zones + Alerts, (E) Automation + Clusters + Account + Privacy. Use `cavecrew-investigator` for lookups, `cavecrew-builder` for 1 to 2 file edits, `code-reviewer` after each phase.
