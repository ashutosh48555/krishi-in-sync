# PRODUCT.md

## What it is
**Krishi Sync (कृषि सिंक)**, code name AgriSync India. A software-only climate monitor for a farm cluster. It pulls live
weather for the cluster's coordinates from Open-Meteo, derives three microclimate zones from it (Polyhouse Nursery,
Open Field, Drip Sector), computes vapor pressure deficit (VPD), raises alerts, and lets a user define IF-THEN rules
that switch simulated actuators (fans, foggers, drip valves).

## Who uses it
| Person | Job to be done | Time they give it |
|---|---|---|
| **INT402 evaluator** (faculty) | Decide in a few minutes whether this is a complete, working, original project | 3 to 5 minutes, often on a laptop, sometimes a phone |
| **Agronomist / farm manager** (the story the project tells) | See which zone needs action right now, and why | A glance, then a decision (vent, irrigate, wait) |

Languages: English, Hindi, Punjabi. Content must hold up in all three (font coverage, text length).

## Evaluator's 60-second path (the page flow must make this obvious)
1. Land on **Home**: one live reading, one sentence on what this does, buttons "Open dashboard" and "Sign in".
2. **Dashboard**: a plain-language status sentence, three zone rows, 24-hour charts.
3. **Sign in** (demo credentials visible, or OpenID Connect) and the header shows the user.
4. **Automation**: add a rule, see it listed.
5. **Alerts**: a populated list with severity and advice.
6. Switch language to Hindi. Nothing breaks.

## Honesty rules (these protect the grade)
- Ambient numbers are **live** (Open-Meteo). Zone numbers are **modelled** from live ambient plus fixed offsets
  (Polyhouse +3.2 °C / +8.5 % RH, Drip −1.2 °C / +4 % RH). Say so in the UI ("Modelled from live ambient") once per page, in the zone section footer.
- Actuators are **simulated**. Label them "Simulated" wherever they appear. Do not write "responds in under 200 ms" or similar claims.
- Never show a default as if it were a reading. If a zone has no recent log, show "No reading yet" and a refresh action, not the target value.
- Targets come from the zone's `TargetTemp` / `TargetHumidity`. Offsets and status are computed, never typed into a view.

## Voice and copy
- Plain verbs, sentence case, active voice. Name things by what the user does: "Refresh readings", not "Sync Telemetry".
- Buttons say what happens: "Add rule", "Save cluster", "Sign in". The success toast reuses the verb: "Rule added".
- Errors say what happened and what to do. No apologies, no vagueness.
- Empty states invite an action: "No rules yet. Add one to switch a fan when humidity passes a limit."
- Navigation labels: Dashboard, Zones, History, Automation, Clusters, Alerts.

## Non-goals
Real hardware, SMS, payments, multi-tenant admin, mobile app, a SPA rewrite. Server-rendered Razor stays.

## Rubric
INT402 rubric: **paste it into PLAN.md section 7**. If it is empty, ask the user for it before Phase 5.
