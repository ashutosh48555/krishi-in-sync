// Live zone telemetry over Neon Realtime (with REST polling fallback for local dev).
// The sealed query (a short-lived bearer capability) comes from the Neon Function,
// which keeps NEON_REALTIME_SECRET server-side. Nothing here can widen the query.
import { createRealtimeClient } from "/lib/neon-realtime.js";

const TELEMETRY_URL = document.querySelector('meta[name="neon-telemetry-url"]')?.content;
const REALTIME_URL = document.querySelector('meta[name="neon-realtime-url"]')?.content;

function fmt(value, digits) {
  return value === null || value === undefined ? "—" : Number(value).toFixed(digits);
}

function updateMarker(pt, vpdVal) {
  if (!pt || vpdVal === null || vpdVal === undefined) return;
  const v = Number(vpdVal);
  const pos = Math.min(Math.max((v / 2.0) * 100, 0), 100).toFixed(1);
  pt.style.left = pos + "%";
  const tone = v < 0.4 || v > 1.6 ? " crit" : (v < 0.8 || v > 1.25 ? " watch" : "");
  pt.className = "pt ready" + tone;
  pt.title = `VPD: ${v.toFixed(2)} kPa`;
}

// Replace the server-rendered numbers in zone rows and zone cards with live values.
function applyReadings(readings) {
  let updated = 0;

  for (const reading of readings) {
    // 1. Dashboard table rows
    const rows = document.querySelectorAll(`tr[data-zone-id="${reading.zone_id}"]`);
    rows.forEach((row) => {
      const map = {
        temp: [reading.temperature, 1],
        hum: [reading.humidity, 0],
        vpd: [reading.vpd, 2],
      };
      for (const [key, [value, digits]] of Object.entries(map)) {
        const cell = row.querySelector(`[data-live="${key}"]`);
        if (cell) {
          cell.textContent = value === null ? "—" : `${fmt(value, digits)}${key === "temp" ? " °C" : key === "hum" ? " %" : ""}`;
        }
        if (key === "vpd") {
          updateMarker(row.querySelector(".pt"), value);
        }
      }
      updated += 1;
    });

    // 2. Topology cards
    const cards = document.querySelectorAll(`article.zc[data-zone-id="${reading.zone_id}"]`);
    cards.forEach((card) => {
      const t = card.querySelector('[data-live="temp"]');
      if (t && reading.temperature != null) t.textContent = fmt(reading.temperature, 1);

      const h = card.querySelector('[data-live="hum"]');
      if (h && reading.humidity != null) h.textContent = fmt(reading.humidity, 0);

      const v = card.querySelector('[data-live="vpd"]');
      if (v && reading.vpd != null) v.textContent = fmt(reading.vpd, 2);

      updateMarker(card.querySelector(".pt"), reading.vpd);
      updated += 1;
    });
  }

  return updated;
}

// Local / REST fallback when Neon Realtime URLs are unset.
async function pollLocalZones() {
  const badge = document.getElementById("live-badge");
  async function poll() {
    try {
      const res = await fetch("/api/zones");
      if (!res.ok) return;
      const zones = await res.json();
      const readings = zones
        .filter((z) => z.latestTelemetry)
        .map((z) => ({
          zone_id: z.zoneId,
          temperature: z.latestTelemetry.temperature,
          humidity: z.latestTelemetry.humidity,
          vpd: z.latestTelemetry.vpd,
        }));
      const n = applyReadings(readings);
      if (badge && n > 0) {
        badge.dataset.state = "live";
        badge.textContent = "Live · synced";
      }
    } catch {
      // transient network or server reload
    }
  }
  poll();
  setInterval(poll, 15000);
}

async function start() {
  const badge = document.getElementById("live-badge");
  if (!TELEMETRY_URL || !REALTIME_URL) {
    pollLocalZones();
    return;
  }

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
