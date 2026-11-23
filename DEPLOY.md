# DEPLOY.md

Current: Render, Docker runtime, free plan, Singapore, auto-deploy from `main`. Database: **Postgres, provider not yet chosen: Supabase or Neon** (decision 23 Nov 2026: full Postgres for all data; the user picks the provider later).
`Dockerfile` and `render.yaml` exist in the repo (checked 23 Nov 2026). Phase 5 edits them: env vars with `sync: false`, `healthCheckPath: /healthz`.
Old SQLite-first plan is archived in `docs/archive/DEPLOY.v1.md`.

## 1. Data tier: Postgres (Supabase or Neon)
**Provider-agnostic by design.** Both are plain Postgres reached through Npgsql, so code and migrations are identical. Only the connection string changes, so switching later is an env-var change.
Copy the exact string from the provider's dashboard (formats below are from memory, check them):
| | Supabase | Neon |
|---|---|---|
| Which string | **Pooler, session mode** (IPv4; direct host is IPv6 only) | **Pooled** host (name contains `-pooler`) for the app |
| Idle behaviour | Free project pauses after about a week idle | Compute scales to zero after about 5 min idle, wakes in about a second |
| Extra surface | REST API exposes `public`: turn RLS on (below) | Data API is off unless enabled: RLS optional, harmless |
| Npgsql form | `Host=<pooler-host>;Port=5432;Database=postgres;Username=postgres.<ref>;Password=<pw>;SSL Mode=Require` | `Host=<endpoint>-pooler.<region>.aws.neon.tech;Database=<db>;Username=<role>;Password=<pw>;SSL Mode=Require` |
- **Migrations connection:** `Migrate()` takes an advisory lock, which a transaction-mode pooler (Neon pooled) can break. Code reads `ConnectionStrings:Migrations` for `Migrate()` when set (use the provider's **direct** string), else `DefaultConnection`.
- A wake-up after idle adds about a second to the first request on Neon: the uptime pinger on `/healthz` avoids it. The user will say which provider to use; until then nothing in the code names a provider.
- Create the database in the region closest to Render (Mumbai or Singapore). The uptime pinger on `/healthz` (it queries the DB) keeps Supabase from pausing and Neon awake. Never commit real connection strings.
- Code: add `Npgsql.EntityFrameworkCore.PostgreSQL` (version compatible with EF Core for net10), remove the SQL Server package and the SQL Server connection probe in `Program.cs`.
  Provider rule: connection string set means Postgres; unset means local SQLite file for `dotnet run` without secrets.
- **Migrations:** delete the old `Migrations/` (generated for another provider), regenerate `InitialCreate` for Postgres, call `Migrate()` at startup instead of `EnsureCreated`. Never mix the two.
- **Timestamps:** use `IstClock.Now` (unspecified kind) everywhere, or Npgsql rejects `DateTime` of kind Utc for `timestamp without time zone`.
- **Row Level Security (Supabase; optional on Neon):** EF creates tables in `public`, which Supabase also exposes through its REST API. The app connects as the database owner and never uses that API, so turn RLS **on with no policies** for every table in a migration
  (`ALTER TABLE … ENABLE ROW LEVEL SECURITY`, DDL inside the migration only, no raw SQL in app code). Then the public REST API can read nothing. Test with a read using the anon key: it must return no rows.
- Seeding is idempotent per table (PLAN.md Phase 1). Data now survives restarts, so a second boot must add no duplicate rows.

## 2. Environment variables (Render, Environment tab)
| Key | Value | Secret |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | no |
| `ConnectionStrings__DefaultConnection` | Supabase pooler or Neon pooled string | **yes** |
| `ConnectionStrings__Migrations` | direct string, only if the pooler breaks `Migrate()` | **yes** |
| `Clerk__FrontendApi` | from AUTH.md | no |
| `Clerk__PublishableKey` | from AUTH.md | no |
| `Clerk__SecretKey` | only if the Backend API fallback is used | **yes** |
Local: `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<value>"`. In `render.yaml` declare every secret with `sync: false`:
```yaml
envVars:
  - key: ConnectionStrings__DefaultConnection
    sync: false
  - key: ConnectionStrings__Migrations
    sync: false
  - key: Clerk__SecretKey
    sync: false
  - key: Clerk__FrontendApi
    sync: false
  - key: Clerk__PublishableKey
    sync: false
```
`healthCheckPath: /healthz` (503 when seeding failed or there are no recent logs, so a broken release does not go live).

## 3. Static assets
Self-host fonts, Chart.js, three.js and icons under `wwwroot/lib`. No CDN at runtime. The only third-party browser requests allowed are Clerk's script and sign-in redirect.
Serve static files with long cache headers and enable response compression. Gzip or brotli matters here: three.js is about 600 KB raw.

## 4. Release steps
1. `git checkout main && git merge --no-ff redesign`
2. `docker build -t krishi . && docker run --rm -p 10000:10000 -e PORT=10000 -e ConnectionStrings__DefaultConnection="$CONN" krishi`, open `http://localhost:10000/healthz`, run the smoke tests against localhost.
3. `git push origin main`. Render auto-deploys. Watch the log until "Live".
4. Add the Render URL to Clerk's allowed origins (AUTH.md section 1) if not done.
5. Run the smoke tests against the live URL.

## 5. Smoke tests (`tools/smoke.sh`, paste results into `docs/EVIDENCE.md`)
```bash
BASE=https://krishi-in-sync.onrender.com
curl -fsS $BASE/healthz | jq .                      # all counts > 0
for p in / /Climate/Dashboard /Climate/Topology /Climate/Analytics /Automation /Alerts /FarmCluster /Account/Login; do
  printf "%-28s" $p; curl -s -o /dev/null -w "%{http_code} %{time_total}s\n" $BASE$p
done                                                # all 200
curl -s $BASE/ | grep -c "cdn\.\|fonts.googleapis\|cdnjs"   # expect 0
```
Then in a browser, in **light and dark**: the 60-second path from PRODUCT.md, the theme switch, the time slider, Clerk sign-in, demo login, Hindi.

## 6. Cold start (free plan sleeps after about 15 minutes idle)
- UptimeRobot (free): HTTP monitor on `…/healthz`, every 5 minutes. This also keeps Supabase from pausing.
- Before the submission window open the site yourself and confirm `/healthz` answers in under 2 s.
- Keep the 90-second screen recording as the fallback to attach.
- If cold start is still a problem, Azure App Service is the .NET-native option (check that Student Pack credit is active first). It replaces the Docker deploy, not the code.

## 7. Rollback
Render dashboard, Deploys, last good deploy, "Rollback". Or `git revert` the merge and push. Migrations are forward-only: write a down migration before releasing a schema change.

## 8. Submission pack (INT402)
Live URL, repo URL, demo credentials, README with screenshots (light and dark), 90-second video, rubric-to-feature table (PLAN.md section 7). Repo public or evaluator has access.

Note: the login rate limiter keys on the client IP. Before relying on it, check which `X-Forwarded-For` hop Render actually sets for the real client address (hop count), since `KnownProxies` is cleared in `Program.cs`.
