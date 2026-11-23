# DEPLOY.md

Current: Render, Docker runtime, free plan, Singapore, auto-deploy from `main`. Service id and URL are in the Render dashboard.
`Dockerfile` and `render.yaml` already exist and work. Only change what is listed here.

## 1. Choose the data tier
| Tier | What | Pick when |
|---|---|---|
| **A (default)** | SQLite in the container, reseeded on every start by the fixed seeder | Grading demo. Sign-in identities live at the provider, so losing the local DB on restart costs nothing important. Fewest moving parts |
| **B (optional)** | PostgreSQL (Supabase free) so clusters, rules and users persist | Only if the evaluator must keep data they add. More risk, do it last |

Do tier A first and ship. Tier B only after Phase 5 is green.

### Tier B notes (read before starting)
- Add `Npgsql.EntityFrameworkCore.PostgreSQL` (version compatible with EF Core for net10). Select the provider with env var `DB_PROVIDER=postgres|sqlite|sqlserver`. **Remove the SQL Server connection probe** in `Program.cs` for non-SqlServer providers; it parses the connection string as a SQL Server string and will throw for a Postgres one.
- Render free instances use IPv4. Supabase's direct database host is IPv6 only, so use the **pooler connection string** (session mode) from the Supabase dashboard.
- Npgsql rejects `DateTime` with `Kind=Utc` for `timestamp without time zone`. Use `IstClock.Now` (PLAN.md Phase 1 step 5) everywhere, or the legacy switch `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)`.
- The repo's `Migrations/` folder was generated for another provider and the app uses `EnsureCreated`. Do not mix. Either delete `Migrations/` and keep `EnsureCreated`, or regenerate migrations for Postgres and call `Migrate()`. Pick one.

## 2. Environment variables (Render dashboard, Environment tab)
| Key | Value | Secret |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | no |
| `Auth__Authority` | from AUTH.md | no |
| `Auth__ClientId` | from AUTH.md | no |
| `Auth__ClientSecret` | from AUTH.md | **yes** |
| `DB_PROVIDER` | `sqlite` (tier A) | no |
| `ConnectionStrings__DefaultConnection` | tier B only | **yes** |

In `render.yaml` declare secrets with `sync: false` so values are never committed:
```yaml
envVars:
  - key: Auth__ClientSecret
    sync: false
  - key: Auth__Authority
    sync: false
  - key: Auth__ClientId
    sync: false
```
Change `healthCheckPath` to `/healthz` (it returns 503 if seeding failed, so a broken release does not go live).

## 3. Release steps
1. `git checkout main && git merge --no-ff redesign`
2. `docker build -t krishi . && docker run --rm -p 10000:10000 -e PORT=10000 krishi` and open `http://localhost:10000/healthz`. Run the smoke tests below against localhost first.
3. `git push origin main`. Render auto-deploys. Watch the deploy log until "Live".
4. Set the provider's callback and logout URLs to the Render URL (AUTH.md section 1) if not done.
5. Run the smoke tests against the live URL.

## 4. Smoke tests (script them in `tools/smoke.sh`, paste results into `docs/EVIDENCE.md`)
```bash
BASE=https://krishi-in-sync.onrender.com
curl -fsS $BASE/healthz | jq .                      # all counts > 0
for p in / /Climate/Dashboard /Climate/Topology /Climate/Analytics /Automation /Alerts /FarmCluster /Account/Login; do
  printf "%-28s" $p; curl -s -o /dev/null -w "%{http_code} %{time_total}s\n" $BASE$p
done                                                # all 200
curl -s $BASE/Climate/Dashboard | grep -c "cdn.jsdelivr\|fonts.googleapis"   # expect 0
```
Then open the live site in a browser and do the 60-second path from PRODUCT.md, including sign-in and Hindi.

## 5. Cold start (the free plan sleeps after about 15 minutes idle)
- Create a free **UptimeRobot** monitor: HTTP(s), URL `…/healthz`, interval 5 minutes. This keeps the instance warm.
- Before the submission window, open the site once yourself and confirm `/healthz` responds in under 2 s.
- Keep the 90-second screen recording as the fallback to attach to the submission.
- If cold start is still a problem: Azure App Service is the .NET-native option, and the Student Pack has included Azure credit. Check that your credit is active before relying on it. It would replace the Docker deploy, not the code.

## 6. Rollback
Render dashboard, Deploys, pick the last good deploy, "Rollback". Or `git revert` the merge commit and push.

## 7. Submission pack (INT402)
Live URL, repo URL, demo credentials, README with screenshots, 90-second video, rubric-to-feature table (PLAN.md section 7). Make sure the repo is public or the evaluator has access.
