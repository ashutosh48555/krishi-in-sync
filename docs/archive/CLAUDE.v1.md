# CLAUDE.md: Krishi Sync (AgriSync India) — master instructions

Repo: https://github.com/ashutosh48555/krishi-in-sync · Live: https://krishi-in-sync.onrender.com
Stack: ASP.NET Core MVC (net10.0), Razor, EF Core, SQLite (SQL Server fallback), Bootstrap 5, Chart.js, Open-Meteo API.
Goal: submit for **INT402**. The site must look finished, show real data on every page, and stay up for the evaluator.

## Read order (do this first, every session)
1. `PRODUCT.md`  what the product is, who it is for, honesty rules
2. `DESIGN.md`   the visual system (tokens, type, layout, charts, copy). Single source of truth for UI
3. `PLAN.md`     phases, file-by-file changes, acceptance checks
4. `AUTH.md`     OpenID Connect sign-in (student-pack provider)
5. `DEPLOY.md`   Render deploy, env vars, smoke tests

## Setup (once, in the repo root)
```bash
# Design skills (verified install commands)
npx -y skills add pbakaus/impeccable --skill impeccable --agent claude-code
npm install -g uipro-cli && uipro init --ai claude      # UI/UX Pro Max (needs python3)
# restart Claude Code so the skills load
```
Impeccable reads `PRODUCT.md` and `DESIGN.md` from the repo root. They are already written, so do **not** let
`/impeccable teach` overwrite them. Merge any changes it proposes by hand.
Use `/impeccable audit`, `/impeccable critique`, `/impeccable polish` (run `/impeccable` to see the exact command
list for the installed version). Use UI/UX Pro Max only to sanity-check type pairing and UX guidelines;
**DESIGN.md wins** on any conflict.

## Working rules
- Work on branch `redesign`. One commit per phase. Merge to `main` only after Phase 5 passes.
- Never claim a step is done without evidence: `dotnet build` output, a Playwright screenshot you actually looked at, or a curl result.
- Secrets (client secret, DB password) only in Render env vars or `dotnet user-secrets`. Never in git, never in logs.
- No CDN assets at runtime. Fonts, Chart.js, icons are self-hosted under `wwwroot/lib`.
- No invented numbers in the UI. Every figure comes from the DB or Open-Meteo (see PRODUCT.md, "Honesty rules").
- Keep the demo login working. Evaluators must be able to sign in without creating anything.
- If something in these files conflicts with reality in the repo, stop, fix the file, then continue.

## Start prompt (paste into Claude Code)
> Read CLAUDE.md, PRODUCT.md, DESIGN.md, PLAN.md, AUTH.md, DEPLOY.md in that order. Run the Setup section. Then execute
> PLAN.md phases 0 to 5 in order on branch `redesign`. After each phase, run its acceptance checks and show me the evidence
> (build output, screenshots at 1440/768/390 wide, curl results). Ask me only for: the auth provider values (AUTH.md step 1),
> and the INT402 rubric if PLAN.md section 7 is still empty. Deploy when Phase 5 is green.

## Definition of done
All boxes in PLAN.md section 6 ticked, with evidence pasted into `docs/EVIDENCE.md`.
