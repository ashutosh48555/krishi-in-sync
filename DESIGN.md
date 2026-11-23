# DESIGN.md: Krishi Sync visual system (v2, approved preview)

Source of truth for the look: **`docs/design-reference/krishi-final-preview.html`**. Open it in Chrome next to the app while building.
It is a reference, not production code: it loads three.js and fonts from CDNs, and all its numbers are samples. Rebuild it in Razor with the
rules below. If this file and the reference disagree, the reference wins on appearance, this file wins on rules (a11y, honesty, no CDN).
Old v1 system (sage ground, Martel serif, five colours) is retired. Copy kept in `docs/archive/DESIGN.v1.md`.

## 1. Direction in one line
**Neutral product UI with one green accent, and a sunlit 3D farm as the hero.** Everything around the 3D model stays quiet, structured and
clean (Linear / Framer discipline). The farm diorama carries the colour and the delight.

## 2. Why this direction (what was compared)
| Reference | Seen | Taken |
|---|---|---|
| Linear | Near-black page, no accent colour, 1px hairlines, pill sign-up button | Neutral palette, hairlines, inverted pill primary button |
| Framer | Black page, huge tight headline, product shown inside a bordered frame | Large tight headline, product in a framed stage |
| Arable (farm data) | White page, plain bold sans headline, one accent, real product UI | One accent, plain farmer-first copy, real data on screen |
| Awwwards "3D" list | Immersive scenes users can drag | Draggable 3D scene as the hero |

Rejected from v1: cream/sage ground, serif display, many accent colours (sarson, blue, orange, teal), heavy shadowed cards, glyph icons (✓ ▲).
Rejected from preview 2: flat monochrome 3D model (looked dull). The user chose the **colourful, sunlit** model of v1/v3.

## 3. Colour tokens (light and dark)
One accent. Status colours appear only on status. Never a second brand colour. Tokens live in `wwwroot/css/tokens.css`.
```css
:root {                      /* light */
  --bg:#FAFAF9; --surface:#FFFFFF; --surface-2:#F4F4F2; --line:#E7E6E2; --line-2:#D3D1CA;
  --text:#12130F; --text-2:#55564F; --text-3:#7A7B72;
  --accent:#15803D; --on-accent:#FFFFFF; --accent-soft:#E7F4EB;
  --watch:#B45309; --crit:#B42318;
  --s1:#2a78d6; --s2:#eb6834; --s3:#1baf7a;   /* chart series, fixed order: Open Field, Polyhouse, Drip */
}
:root[data-theme="dark"] {
  --bg:#0A0B0A; --surface:#121412; --surface-2:#1A1D1A; --line:#242724; --line-2:#363A36;
  --text:#F3F4F1; --text-2:#A6A99F; --text-3:#7D8077;
  --accent:#4ADE80; --on-accent:#052E16; --accent-soft:rgba(74,222,128,.12);
  --watch:#FBBF24; --crit:#F87171;
  --s1:#3987e5; --s2:#d95926; --s3:#199e70;
  color-scheme: dark;
}
```
Also provide the same dark values under `@media (prefers-color-scheme: dark) { :root:not([data-theme="light"]) { … } }` so no-JS users get their OS theme.
Rules: `body` always sets `background: var(--bg)`. Components use tokens only, never literal colours. 3D scene colours are the only literals (they live in JS).
**Contrast is not yet verified.** Values are taken from the preview. Phase 5 runs axe and a contrast check on every text/background pair and fixes tokens, not components.

## 4. Type
| Role | Family | Weights | Notes |
|---|---|---|---|
| Display, big numbers | **Inter Tight** | 600 (700 for the brand) | `letter-spacing: -0.035em`, `line-height: 1.05`, `text-wrap: balance` |
| UI, body, tables | **Inter** | 400, 500, 600 | 15.5px body, 1.55 line height |
| Hindi | **Noto Sans Devanagari** | 400, 500, 600 | In `:lang(hi)` set headline `letter-spacing: -0.01em`, `line-height: 1.2` |
| Punjabi | **Noto Sans Gurmukhi** | 400, 500, 600 | Not in the preview. Add in Phase 3 and test `lang="pa"` |
Self-host via `@fontsource` packages, woff2 for `latin`, `devanagari`, `gurmukhi`, `font-display: swap`, under `wwwroot/lib/fonts`.
Numbers in readings and tables: `font-variant-numeric: tabular-nums`. No all-caps labels. Headline scale: `clamp(34px, 4.6vw, 56px)`.
Section titles 28px, card titles 13px `--text-3` (500), metric values 30px display.

## 5. Layout
- Page frame: max **1240px**, side gutter `max(16px, env(safe-area-inset-*))`. Base spacing 4px.
- Radii: cards 16px, 3D stage 28px, buttons and pills 999px, inputs 10px, ledger rows 0.
- Cards: 1px `--line` border, `--surface` fill, **no shadow**, 22px padding. Hover on interactive cards: border `--line-2`.
- **Bento grid**: 12 columns, 14px gap. Spans used: 5/7, 8/4, 12. Under 980px every card is full width.
- Sticky nav with blur (`color-mix(var(--bg) 82%, transparent)` + `backdrop-filter: blur(14px)`), safe-area aware.

### Navigation (all pages)
`[logo mark] Krishi Sync   Dashboard  Zones  History  Automation  Clusters  Alerts      [EN|हिं|ਪੰ]  [sun/moon]  [Sign in]`
Current page = `--surface-2` pill background. Logo mark = 28px rounded square, `--accent` fill, sprout icon. Links scroll horizontally under 980px, on their own row.
Language control is a segmented pill (active segment inverted). Sign in = inverted pill (`--text` fill, `--bg` text).

### Home (`/`), matches the reference
Two columns, vertically centred, 5fr / 7fr, gap 40px:
- **Left:** live chip (`Live · Delhi · 24.2 °C · 68 % · VPD 0.97`, pulsing accent dot), H1 "Know which zone needs you, today.", lead sentence, buttons
  "Open dashboard" (inverted pill + arrow) and "Use demo login" (outline pill), one-line honesty note.
- **Right:** the 3D stage (section 6). Height `clamp(540px, 80vh, 740px)`.
- Below: bento row **Right now** (plain-language headline sentence + "What to do" advice card) and **Selected zone** (four metrics with icons),
  then the **Zones** table, then **Temperature, last 24 hours** (span 8) beside **Alerts** (span 4).
Under 980px: copy first, stage below (height `clamp(480px, 72vh, 620px)`), bar wraps.

### Dashboard (`/Climate/Dashboard`)
Same bento vocabulary without the marketing hero and without the 3D stage (3D lives on Home only, so this page stays light):
header row (farm name, farm/crop selectors, "Updated 13:42 IST", Refresh readings), headline sentence, advice card, zones table, two stacked charts
(temperature; VPD with optimal band), alerts card.

### Other pages (bento cards, hairline tables, same icons)
- **Zones:** one card per zone (metrics, actuator "Simulated", sensor node table).
- **History:** filter row, summary as a definition list, four small charts sharing the x range, "Download CSV" text button.
- **Automation:** rules table + "Add rule" form card beside it (stacked on mobile). Actuators labelled "Simulated".
- **Clusters:** table (name, region, acres, coordinates, crops, Open dashboard). "Add cluster" button top right.
- **Alerts:** grouped by severity, icon + headline + advice + time.
- **Sign in:** centred 400px column. Order: "Continue with Clerk", divider "or use the demo account", demo form, demo credentials as a plain note.
- **Privacy:** short real policy (see PLAN.md).

## 6. The 3D farm hero (signature element)
Built with **three.js r128**, self-hosted at `wwwroot/lib/three/three.min.js`, scene code in `wwwroot/js/farm3d.js` (≤300 lines, split per zone if needed).
**Honest by design:** the model is an illustration of the cluster. The zone readings come from the DB/Open-Meteo, the fan and drip drops are **Simulated** (label stays in the selected-zone card).

**Scene**
- Floating cream platform (two slabs), three plots left to right: **Polyhouse Nursery** (glass half-cylinder with 7 ribs, plants, spinning fan, warm lamp at night),
  **Open Field** (10 ridges, ~200 instanced crop cones), **Drip Sector** (8 ridges, instanced crops, blue drip lines, moving drops, valve and pipe). Four low-poly trees.
- Selected zone lifts 0.22 units and gets a sarson-yellow edge outline. Fan spins only when the polyhouse is above target (computed, not hard-coded).
- Camera: perspective 35°, elevation 0.5 rad, distance `max(18.5, 17 / (0.63 × aspect))`, looks at `(0, 0.9, 0)`. Slow auto-sway, drag to rotate, click a plot to select (raycast).
- Floating glass pins show zone names (HTML, projected from 3D). Hint chip top-left: "Drag to turn · click a zone".

**Sun, sky and time of day** (the effect the user loves)
- Control bar (bottom centre, glass): three zone buttons (keyboard path for selection) and a **time slider 00:00 to 24:00** with a Dawn/Day/Dusk/Night label.
- Directional light follows sun (06 to 18) or moon (18 to 06) on an arc, with real shadows (PCF soft, 2048 map). Sun colour warms near the horizon.
  Hemisphere fill and a camera-side fill light keep faces readable. Polyhouse lamp turns on at night.
- Sky is a CSS gradient behind the transparent canvas, interpolated from keyframes (hour: top / bottom):
  `0 #050A1C/#16233F · 4.5 #0B1530/#26365C · 6 #4A68A8/#F4A877 · 8 #62A8EE/#BFE0FB · 12.5 #3F94E6/#B7DEFF · 16.5 #5B9FE3/#F6DFB4 · 18.3 #3E4F8F/#F08A58 · 19.5 #141F45/#3B3F6E · 21 #070D24/#1A2845 · 24 #050A1C/#16233F`.
- Layers (back to front): hills, stars (night only), sun or moon disc with glow (moon is smaller), light rays (**day only**), drifting clouds, canvas, colour grade (warm at dawn/dusk, blue at night).
- The disc position is computed from the sun direction and camera angle, clamped inside the frame.

**Performance and fallback**
- Pixel ratio max 2. On touch devices use shadow map 1024. Pause rendering when the stage is off-screen (`IntersectionObserver`) or the tab is hidden.
- No WebGL: hide the canvas, show the fallback message; the zones table below carries the same data.
- `prefers-reduced-motion`: no auto-sway, no fan spin, no drop motion, no drifting clouds, instant theme switch.
- Stage has `role="img"` with an `aria-label`; zone buttons and the slider are real controls with labels.

## 7. Theme system and the switch animation
- Attribute `data-theme="light|dark"` on `<html>`. An inline script in `<head>` sets it before first paint (localStorage key `ks-theme`, else OS preference) to avoid a flash.
- The toggle is a 36px round icon button: sun and moon icons cross-rotate and scale (600 ms, overshoot easing).
- **Switch effect** (Chrome and other View Transitions browsers), total ~1.1 s:
  1. Feathered **circular reveal** of the new theme from the button, 160px soft edge, via `::view-transition-new(root)` mask and registered `@property --r`.
  2. The old theme **blurs (6px) and shrinks (0.985)** underneath.
  3. A **ring** expands from the button and 18 **sparks** fly out: warm yellow and orange going to light, pale blue and white going to dark.
  4. The 3D sky runs on to **21:30 night** (going dark) or **13:00 day** (going light) with an eased time sweep, so the sun sets or rises as the page changes.
- Fallbacks: no View Transitions support gives a 350 ms colour crossfade (CSS transitions on background, border, colour). Reduced motion gives an instant switch.
- Preview status: the end states were checked in Chrome. The reveal itself was **not seen mid-animation**, so tune duration and feather in the browser.

## 8. Components
- **Button primary:** inverted pill (`--text` fill, `--bg` text), 14px 500, 10 × 18px padding, lifts 1px on hover. **Secondary:** transparent, 1px `--line-2` border. Focus ring: 2px `--accent`, 2px offset, always visible.
- **Chip:** pill, 1px `--line`, `--surface`, 13px, optional pulsing dot.
- **Metric:** label (13px `--text-3`) with 16px icon above a 30px display value and a small unit.
- **Zones table row:** grid `1.4fr .9fr .7fr 2fr 1fr`, 14px vertical padding, hairline top, hover `--surface-2`, selected adds a 2px inset `--accent` bar on the left. Whole row is a button (Enter and Space work).
- **VPD track:** 8px high, 5 segments at 0 / 0.4 / 0.8 / 1.25 / 1.6 / 2.0 kPa in `--line`; the optimal segment (0.8 to 1.25) is `--accent-soft` with a 1px `--accent` inset border.
  A 16px round marker (surface fill, 2px border: accent, watch or crit) slides to the value on first load (1.1 s). Column header states "optimal 0.8 to 1.25".
- **Status pill:** icon + word + value, tinted background (accent-soft, or 14 % watch or crit). Never colour alone.
- **Advice card ("What to do"):** `--accent-soft` fill, bulb icon, one plain action sentence in the user's language, one "Why" line. Derived from zone status rules, never invented (see PRODUCT.md).
- **Alerts list:** 30px rounded icon tile, 14.5px 500 title, 13.5px `--text-2` advice, hairline between items.
- **Inputs:** 40px, 1px `--line-2`, 10px radius, label above, error text below in `--crit` with icon. **Toast:** top right, `--text` fill, 4s, `role="status"`.
- Pins and floating bar use `--surface` at 72 to 78 % with `backdrop-filter: blur`.

## 9. Icons
Lucide-style line icons, **inline SVG**, 24px grid, `stroke-width: 1.75`, round caps and joins, `currentColor`, `aria-hidden` when decorative.
Used: sun, moon, thermometer, droplet, wind, power, bell, triangle-alert, check, arrow-right, sprout, layout-grid, list, line-chart, move, lightbulb.
Keep them in one partial `Views/Shared/_Icon.cshtml` (switch on name). Lucide is ISC licensed: keep the licence line in `wwwroot/lib/NOTICE.txt`. Bootstrap Icons and every glyph character (✓ ▲ ▼) are removed.

## 10. Motion (all disabled by `prefers-reduced-motion`)
| Moment | Spec |
|---|---|
| Hover and focus colour | 200 ms |
| Live dot | 2.4 s pulse ring |
| Nav link, button lift | 200 ms |
| VPD marker | 1.1 s `cubic-bezier(.2,.8,.2,1)` once on load |
| 3D | sway, fan, drops, clouds, sun arc |
| Theme switch | section 7 |
| Number or card entrance | none (page is complete at rest) |

## 11. Charts (dataviz rules)
- Chart.js 4 self-hosted at `wwwroot/lib/chart.umd.min.js`. Animations off. Each canvas or SVG has `role="img"` and an `aria-label` with range and latest values. "View data as table" toggle under every chart.
- **Zone comparison** (Home, Dashboard): the **selected zone is accent with a soft area fill**, the other zones are `--line-2` grey, all with end-of-line direct labels. Lines 2.5px (selected) and 1.75px. Gridlines `--line`.
- **When all zones must be told apart** (History): use `--s1/--s2/--s3` in fixed order plus direct labels and the table toggle. Aqua is low contrast on light, so labels and the table are mandatory.
- **No dual axis.** Temperature and VPD are separate charts on a shared x range. VPD chart shades 0.8 to 1.25 kPa with `--accent-soft` and labels "Optimal 0.8 to 1.25".
- Hover: crosshair and tooltip listing all series for that hour; hit area larger than the mark.

## 12. Languages
English, Hindi, Punjabi from the segmented control. Text must fit in all three (Hindi runs about 20 % longer). Zone names, status words, advice, alerts and the time-of-day label are translated.
The preview has English and Hindi only: add Punjabi strings in Phase 3. Store strings in `.resx` or one `Localizer` class, not inline in views.

## 13. Accessibility floor
Keyboard reachable everywhere, visible focus, skip link, `lang` follows the selected language, text contrast ≥ 4.5:1, tap targets ≥ 40px,
reflow at 320px, 200 % zoom without horizontal scroll, reduced motion respected, tables use `<th scope>`, 3D has a text equivalent (the zones table).

## 14. Done when
axe reports 0 serious or critical on every page in **both themes**; Lighthouse mobile accessibility ≥ 95 and performance ≥ 80 (Home, with 3D);
screenshots at 1440, 768, 390 in light and dark look intentional (no wrapped nav, no clipped labels, no empty charts, 3D centred in its column);
the theme switch works with and without View Transitions; the page makes no request to a CDN at runtime.
