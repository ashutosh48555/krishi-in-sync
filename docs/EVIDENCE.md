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
