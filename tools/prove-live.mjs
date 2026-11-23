// End-to-end proof of the Neon Realtime wiring on the real dashboard page.
// 1. loads /Climate/Dashboard, waits for the live badge
// 2. inserts a SensorLogs row straight into Postgres
// 3. asserts the zone row in the DOM changed without a reload
// Run: node tools/prove-live.mjs
import { chromium } from "playwright";
import { execFileSync } from "node:child_process";

const BASE = process.env.BASE ?? "http://127.0.0.1:5099";

const browser = await chromium.launch();
const page = await browser.newPage();
const errors = [];
page.on("console", (m) => {
  if (m.type() === "error") errors.push(m.text());
});
page.on("pageerror", (e) => errors.push(e.message));

await page.goto(`${BASE}/Climate/Dashboard`, { waitUntil: "load" });

const badge = page.locator("#live-badge");
await page.waitForFunction(() => {
  const b = document.getElementById("live-badge");
  return b && (b.dataset.state === "live" || b.dataset.state === "error");
}, { timeout: 30_000 });

console.log(`BADGE: state=${await badge.getAttribute("data-state")} text="${(await badge.textContent())?.trim()}"`);
console.log(`ZONE 1 temp before: ${(await page.locator('tr[data-zone-id="1"] [data-live="temp"]').first().textContent())?.trim()}`);

// One decimal, because that is what the table renders.
const temp = (Math.random() * 40 + 35).toFixed(1);
const out = execFileSync(
  "bash",
  [
    "-lc",
    `cd tools/insert-log && TEMP=${temp} U=$(grep '^DATABASE_URL=' ../../.env.local | sed 's/^DATABASE_URL=//; s/^"//; s/"$//') dotnet run`,
  ],
  { encoding: "utf8" },
);
console.log(out.trim());

await page.waitForFunction(
  (want) => document.querySelector('tr[data-zone-id="1"] [data-live="temp"]')?.textContent.includes(want),
  temp,
  { timeout: 30_000 },
);

console.log(`ZONE 1 temp after: ${(await page.locator('tr[data-zone-id="1"] [data-live="temp"]').first().textContent())?.trim()}`);
console.log(`BADGE after: ${(await badge.textContent())?.trim()}`);
console.log("CONSOLE ERRORS:", errors.length ? errors.join(" | ") : "none");

await page.screenshot({ path: "/tmp/dashboard_live.png" });
await browser.close();
