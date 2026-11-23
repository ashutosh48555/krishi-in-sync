# CLAUDE.md: Krishi Sync (AgriSync India) — master instructions

Repo: https://github.com/ashutosh48555/krishi-in-sync · Live: https://krishi-in-sync.onrender.com
Stack: ASP.NET Core MVC (net10.0), Razor, EF Core, **Supabase Postgres** (SQLite for local runs without secrets), **Clerk** sign-in plus demo login, Chart.js, **three.js** (3D farm hero), Open-Meteo API.
Goal: submit for **INT402**. The site must look finished, show real data on every page, and stay up for the evaluator.

## Read order (do this first, every session)
1. `PRODUCT.md`  what the product is, who it is for, honesty rules
2. `DESIGN.md`   the visual system v2 (tokens, type, layout, 3D hero, theme switch, charts). Appearance reference: `docs/design-reference/krishi-final-preview.html`
3. `PLAN.md`     phases, file-by-file changes, acceptance checks
4. `AUTH.md`     Clerk sign-in (hosted pages + JWT) and the demo account
5. `DEPLOY.md`   Render + Supabase deploy, env vars, smoke tests

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
- No CDN assets at runtime. Fonts, Chart.js, three.js, icons are self-hosted under `wwwroot/lib`. The only allowed third-party browser request is Clerk (AUTH.md). The design reference file uses CDNs; the app must not.
- No invented numbers in the UI. Every figure comes from the DB or Open-Meteo (see PRODUCT.md, "Honesty rules").
- Keep the demo login working. Evaluators must be able to sign in without creating anything.
- Match the approved design: open the reference file before changing any page. Both light and dark must be checked for every UI change.
- Razor files, CSS and JS files stay under 300 lines each. Split partials and modules instead.
- If something in these files conflicts with reality in the repo, stop, fix the file, then continue.

## Start prompt (paste into Claude Code)
> Read CLAUDE.md, PRODUCT.md, DESIGN.md, PLAN.md, AUTH.md, DEPLOY.md in that order. Run the Setup section. Then execute
> PLAN.md phases 0 to 5 in order on branch `redesign`. After each phase, run its acceptance checks and show me the evidence
> (build output, screenshots at 1440/768/390 wide, curl results). Ask me only for: the Supabase pooler string (DEPLOY.md), the Clerk Frontend API URL and keys (AUTH.md step 1),
> and the INT402 rubric if PLAN.md section 7 is still empty. Deploy when Phase 5 is green.

## Definition of done
All boxes in PLAN.md section 6 ticked, with evidence pasted into `docs/EVIDENCE.md`.
