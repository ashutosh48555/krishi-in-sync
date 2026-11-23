# Design reference

`krishi-final-preview.html` is the approved Krishi Sync design (v4). Open it in Chrome (double-click, or `python3 -m http.server` in this folder) and compare it with the app after every UI change.

What it shows: headline left, sunlit 3D farm right; light and dark themes with the circular-reveal switch; time-of-day slider (sun, moon, stars, clouds); zones table with VPD track; selected-zone card; advice card; 24 h chart; alerts; English and Hindi.

How to use it:
- **Appearance:** the file wins. Match spacing, colours, motion and the 3D scene.
- **Rules:** `DESIGN.md` wins on a11y, honesty, no CDN, file size. The preview loads three.js and fonts from CDNs and uses sample numbers. The app must self-host assets and read real data.
- Scene code (`build()`, `setHour()`, `skyPos()`) and the theme switch (`setTheme()`, `burst()`) are the starting point for `wwwroot/js/farm3d.js` and `wwwroot/js/theme.js`.
- Version history: v1 and v2 were rejected, v3 added the sunlit scene, v4 is the final layout.
