# DESIGN.md: Krishi Sync visual system

## 1. Direction in one line
A **field ledger for climate**: calm, ink-on-mist pages where each zone is a row you read left to right, and one
instrument (the VPD band) does the talking. It should feel like an agromet bulletin made for Indian farms, not a SaaS template.

## 2. What we rejected, and why (design review against the brief)
| First instinct | Why it was dropped |
|---|---|
| Cream page, serif headline, terracotta accent | Generic "warm editorial" default; also the exact accent colour many generated pages use |
| Near-black page with one acid-green accent | Reads as "monitoring dashboard" for any industry |
| Current look: green gradient navbar, tricolor stripe, identical rounded cards with coloured top borders | The SaaS card kit; the flag stripe is decoration and misuses national colours |
| Eyebrow labels in caps above every heading, monospace data labels | Template chrome with no information in it |
| One chart with two y-axes (°C and kPa) | Dual axis misleads. Split into two charts that share the time axis |

**The one bold move:** the **VPD band**, a horizontal scale divided at the agronomic limits (0.4 / 0.8 / 1.25 / 1.6 kPa, from README) with a marker per zone. Everything else stays quiet.

## 3. Colour tokens (all validated)
Names come from the subject: indigo ink (neel), paddy (dhaan), mustard bloom (sarson), monsoon mist.

```css
:root {
  /* surfaces */
  --ground:   #F3F6F2;  /* monsoon mist, page background */
  --panel:    #FFFFFF;  /* chart panels, menus only */
  --rule:     #D9DFD8;  /* hairlines */
  --rule-2:   #B9C2B8;  /* axes, input borders */
  /* ink (text on --ground: 13.2 / 6.9 / 5.0 : 1) */
  --ink:      #1B2559;  /* neel: headings, numbers, nav */
  --ink-2:    #4A5568;  /* body secondary */
  --ink-3:    #5F6B7A;  /* captions, units */
  /* brand */
  --dhaan:    #2B6A3B;  /* primary action, links (6.0:1; white on it 6.5:1) */
  --dhaan-wash:#E4EFE5; /* selected row, hover wash */
  --sarson:   #E3A400;  /* live dot, focus ring accent. never as text on light */
  /* status text (all >= 5.4:1 on --ground). ALWAYS paired with icon + label */
  --good:     #1F6B3A;
  --watch:    #8A5A00;
  --critical: #A8321F;
  --info:     #245BA8;
  /* chart series: fixed order, never cycled (validated, see section 8) */
  --s1: #2a78d6; /* Open Field  */
  --s2: #eb6834; /* Polyhouse   */
  --s3: #1baf7a; /* Drip Sector */
}
```
Dark mode is **optional, last** (Phase 5 stretch). If done: ground `#141B2D`, ink `#E8ECF3`, ink-2 `#B4BDCC`, green `#6FCB86`,
critical `#FF8F7A` (all validated >= 7.7:1), series `#3987e5 / #d95926 / #199e70`.

## 4. Type
Chosen because Hindi and Punjabi must look as intentional as English.
| Role | Family | Notes |
|---|---|---|
| Display, big readings, page titles | **Martel** (400, 700) | Sturdy serif with real Devanagari. Latin numerals look like printed gauges |
| UI, body, tables | **Mukta** (400, 500, 700) | Latin + Devanagari |
| Punjabi text (`lang="pa"`) | **Mukta Mahee** | Gurmukhi companion to Mukta |

Self-host via npm: `@fontsource/martel`, `@fontsource/mukta`, `@fontsource/mukta-mahee`. Copy the needed `latin`,
`devanagari`, `gurmukhi` woff2 files into `wwwroot/lib/fonts`, `font-display: swap`. Set `font-family` by `:lang()`.

Scale (rem at 16px): `0.8125 / 0.9375 / 1 / 1.25 / 1.75 / 2.5 / 4`. Body 1rem / 1.6. Headings 1.2 line-height, Martel 700.
Max line length 68ch. Numerals in tables and readings: `font-variant-numeric: tabular-nums` (verify Mukta supports it; otherwise use Martel for readings).
No all-caps labels. No single highlighted word in headlines.

## 5. Layout
Page frame: left-aligned, max 1200px, 24px gutters (16px under 640px). 4px spacing base:
`4 8 12 16 24 32 48 72`. Radii by role: buttons 6px, pills 999px, chart panels 10px, ledger rows 0.

### Navigation (all pages)
```
Krishi Sync  कृषि सिंक     Dashboard  Zones  History  Automation  Clusters  Alerts      EN | हिं | ਪੰ    Sign in
────────────────────────────────────────────────────────────────────────────────────────────────────────────
```
Plain ink text on `--ground`, a 2px `--dhaan` underline on the current page. No gradient, no flag stripe.
Under 900px: links collapse into a menu button; language and Sign in stay visible. Live clock is removed
(replace with "Updated 13:42 IST" on the dashboard).

### Home (replaces the default template page)
```
Delhi NCR right now (live, Open-Meteo)
 24.2 °C   68 % humidity   VPD 0.92 kPa            <- Martel 64 for temperature, rest 20

Krishi Sync watches the climate of three zones on a farm and tells you which one needs action.
[ Open dashboard ]  [ Sign in ]

Three short paragraphs: What it reads / What it decides / What it controls (each 2 sentences, plain)
```

### Dashboard
```
IARI Pusa Smart Farm, Delhi NCR     [Farm v] [Crop v]                    Updated 13:42 IST  [Refresh readings]

Polyhouse is 2.3 °C above target. Open field and drip sector are on target.        <- Martel 40, one sentence, computed
Ambient from Open-Meteo at 28.61 N, 77.21 E. Zones modelled from it.               <- ink-3, 15px

Zone              Now      Target    Humidity   VPD band (kPa)                          Status
-----------------------------------------------------------------------------------------------------
Polyhouse Nursery 26.8 C   24.5 C    76 %       0.4  0.8   1.25  1.6                    ! Above target
                                                |....|======|....|...      o 0.92
Open Field        26.0 C   26.0 C    60 %       ...                                      ok On target
Drip Sector       22.0 C   22.0 C    65 %       ...                                      ok On target
-----------------------------------------------------------------------------------------------------

Temperature, last 24 h                                    | Alerts (3 newest)
[ line chart, 3 series, direct end-labels ]               | severity icon + sentence + time
VPD, last 24 h  [ line + shaded optimal band 0.8 to 1.25 ]| [ All alerts ]
[ View data as table ]
```
Rows are separated by `--rule` hairlines, not boxed. Status = icon + word, never colour alone.
Under 760px each zone row becomes a stacked block (name and status first, then numbers two per line, band full width).

### Other pages
- **Zones**: one section per zone, same ledger vocabulary, sensor node health as a plain table.
- **History**: filters in one row (date range, zone, Apply). Summary figures as a 6-column definition list (not 6 cards). One small chart per measure (temperature, humidity, soil moisture, VPD), stacked, sharing the x axis. Download CSV is a text button.
- **Automation**: soil summary on top as a definition list, then the rules table, then "Add rule" as a form beside it (stacked on mobile). Actuators show "Simulated".
- **Clusters**: a table, not a card grid, with name, region, acres, coordinates, crops, "Open dashboard". "Add cluster" button top right.
- **Alerts**: grouped by severity, each alert is a row with icon, headline, advice, time. Bulletins below as plain articles.
- **Sign in / Register**: single centred column 400px, no card chrome. Demo credentials as a plain note under the form.

## 6. Components
- **Button**: primary = `--dhaan` fill, white text, 6px, 40px tall; secondary = 1px `--rule-2` outline, ink text; text button = dhaan underline on hover. Focus: 2px `--ink` ring with 2px `--ground` gap, always visible.
- **Status**: icon (check / triangle / octagon) + word + colour. Good "On target", Watch "Above target" or "Dry air", Critical "Act now".
- **Inputs**: 40px, 1px `--rule-2`, 6px radius, label above in ink-2 (sentence case), error text below in `--critical` with icon.
- **Ledger row**: 56px min height, bottom hairline, hover `--dhaan-wash`. Rows that link are fully clickable and have a text label.
- **Chart panel**: `--panel`, 1px `--rule`, 10px radius, 24px padding. This is the only boxed surface on the dashboard.
- **Toast**: top right, ink background, white text, 4s, `role="status"`.
- Icons: Bootstrap Icons, **self-hosted**, SVG sprite or font from `wwwroot/lib`. Decorative icons `aria-hidden`.

## 7. Motion
One moment only: on first dashboard load, each VPD marker travels from the left end to its value (600 ms, ease-out).
Everything else is instant, except hover and focus colour changes (120 ms). `prefers-reduced-motion: reduce` disables all of it. Chart.js animations off.

## 8. Charts (follow the dataviz skill)
- Series colours `--s1/--s2/--s3` in that fixed order. Validated: light surface passes lightness, chroma, CVD (worst pair ΔE 9.2) and normal-vision checks.
  **Aqua `#1baf7a` is 2.74:1 on the light surface**, so direct end-labels and the "View data as table" toggle are mandatory.
- Legend present (3 series) **and** end-of-line direct labels. Text uses ink tokens, never the series colour.
- Lines 2px, no markers except on hover (8px with 2px panel ring). Gridlines `--rule`, axis `--rule-2`. Y axis starts where the data needs, state units in the axis title.
- **No dual axis.** Temperature and VPD are separate charts on the same x range.
- VPD chart shades 0.8 to 1.25 kPa (optimal) with a pale `--dhaan-wash` band and labels the band "Optimal 0.8 to 1.25".
- Hover: crosshair plus tooltip listing all series at that hour. Hit area larger than the mark.
- Chart.js loaded from `wwwroot/lib/chart.umd.min.js`. Each chart has `role="img"` plus an `aria-label` that states the range and the latest values.

## 9. Accessibility floor
Keyboard reachable everywhere, visible focus, skip link, `lang` attribute follows the selected language, text contrast >= 4.5:1,
tap targets >= 40px, reflow at 320px, zoom to 200% without horizontal scroll, reduced motion respected, tables use `<th scope>`.

## 10. Done when
Impeccable's audit/critique shows no high-severity findings, axe reports 0 serious/critical, Lighthouse accessibility >= 95,
and screenshots at 1440, 768, 390 look intentional (no wrapped nav, no clipped labels, no empty charts).
