// Live zone telemetry over Neon Realtime.
// The sealed query (a short-lived bearer capability) comes from the Neon Function,
// which keeps NEON_REALTIME_SECRET server-side. Nothing here can widen the query.
import { createRealtimeClient } from "/lib/neon-realtime.js";

const TELEMETRY_URL = document.querySelector('meta[name="neon-telemetry-url"]')?.content;
const REALTIME_URL = document.querySelector('meta[name="neon-realtime-url"]')?.content;

function fmt(value, digits) {
  return value === null || value === undefined ? "—" : Number(value).toFixed(digits);
}

// Replace the server-rendered numbers in a zone row with live values.
function applyReadings(readings) {
  let updated = 0;

  for (const reading of readings) {
    const row = document.querySelector(`tr[data-zone-id="${reading.zone_id}"]`);
    if (!row) continue;

    const map = {
      temp: [reading.temperature, 1],
      hum: [reading.humidity, 0],
      vpd: [reading.vpd, 2],
    };

    for (const [key, [value, digits]] of Object.entries(map)) {
      const cell = row.querySelector(`[data-live="${key}"]`);
      if (!cell) continue;
      cell.textContent = value === null ? "—" : `${fmt(value, digits)}${key === "temp" ? " °C" : key === "hum" ? " %" : ""}`;
      updated += 1;
    }
  }

  return updated;
}

async function start() {
  const badge = document.getElementById("live-badge");
  if (!TELEMETRY_URL || !REALTIME_URL) return;

  const setState = (state, text) => {
    if (!badge) return;
    badge.dataset.state = state;
    badge.textContent = text;
  };

  // 1. Ask the Function to seal the query (server holds the Realtime secret).
  const response = await fetch(`${TELEMETRY_URL}/live-query`, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: "{}",
  });
  if (!response.ok) throw new Error(`seal failed: ${response.status}`);
  const sealed = await response.json();

  // 2. Subscribe over the shared WebSocket; rows arrive as committed transactions push.
  const client = createRealtimeClient({ url: REALTIME_URL });
  const subscription = client.subscribe(sealed);

  let refreshTimer;

  const refreshQuery = async () => {
    const next = await fetch(`${TELEMETRY_URL}/live-query`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: "{}",
    });
    if (!next.ok) return undefined;
    return next.json();
  };

  subscription.onChange((snapshot) => {
    if (snapshot.status === "live" && snapshot.data) {
      const n = applyReadings(snapshot.data);
      setState("live", n > 0 ? `Live · ${n} readings` : "Live");
    } else if (snapshot.status === "connecting") {
      setState("connecting", "Connecting");
    } else if (snapshot.status === "stale") {
      setState("stale", "Reconnecting");
    } else if (snapshot.status === "error") {
      setState("error", "Live updates stopped");
    }
  });

  // Sealed queries expire in ~60s; renew before that so the page stays live unattended.
  refreshTimer = setInterval(async () => {
    try {
      const renewed = await refreshQuery();
      if (renewed) await subscription.renew(renewed);
    } catch {
      clearInterval(refreshTimer);
      setState("error", "Live updates stopped");
    }
  }, 45_000);

  window.addEventListener("pagehide", () => {
    clearInterval(refreshTimer);
    subscription.unsubscribe();
    client.close();
  });
}

start().catch((error) => {
  const badge = document.getElementById("live-badge");
  if (badge) {
    badge.dataset.state = "error";
    badge.textContent = "Live updates unavailable";
  }
  console.error("[live]", error);
});
