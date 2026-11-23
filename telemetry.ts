import { createRealtime, rawSql } from "@neon/realtime/server";
import { Pool } from "pg";

// One pool per isolate, reused across requests (Neon Functions are long-running).
const pool = new Pool({
  connectionString: process.env.DATABASE_URL!,
  max: 5,
});
// An idle disconnect when the compute scales to zero must not kill the isolate.
pool.on("error", (error) => console.error("[pg]", error));

const DATABASE = new URL(process.env.DATABASE_URL!).pathname.slice(1);

const realtime = createRealtime({
  secret: process.env.NEON_REALTIME_SECRET!,
  db: DATABASE,
});

export type ZoneReading = {
  zone_id: number;
  zone_name: string;
  crop_type: string;
  temperature: number | null;
  humidity: number | null;
  vpd: number | null;
  soil_moisture: number | null;
  timestamp_ist: string | null;
};

// Latest reading per zone: one deterministic SELECT (no now(), no side effects).
const latestReadings = (zoneId?: number) =>
  rawSql<ZoneReading>(
    `SELECT z."ZoneId" AS zone_id, z."Name" AS zone_name, z."CropType" AS crop_type,
            s."Temperature" AS temperature, s."Humidity" AS humidity, s."VPD" AS vpd,
            s."SoilMoisture" AS soil_moisture, s."TimestampIST" AS timestamp_ist
       FROM "Zones" z
       LEFT JOIN LATERAL (
         SELECT "Temperature", "Humidity", "VPD", "SoilMoisture", "TimestampIST"
           FROM "SensorLogs" sl
          WHERE sl."ZoneId" = z."ZoneId"
          ORDER BY sl."TimestampIST" DESC
          LIMIT 1
       ) s ON TRUE
      WHERE $1::int IS NULL OR z."ZoneId" = $1
      ORDER BY z."ZoneId"`,
    [zoneId ?? null],
  );

const CORS = {
  "access-control-allow-origin": "*",
  "access-control-allow-headers": "content-type, authorization",
  "access-control-allow-methods": "GET, POST, OPTIONS",
};

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json", ...CORS },
  });

export default {
  async fetch(request: Request): Promise<Response> {
    const url = new URL(request.url);

    // CORS: the dashboard and any preview origin may call this.
    if (request.method === "OPTIONS") {
      return new Response(null, { status: 204, headers: CORS });
    }

    try {
      // GET /readings?zoneId=1 — plain REST snapshot straight from Postgres.
      if (url.pathname === "/readings") {
        const raw = url.searchParams.get("zoneId");
        const zoneId = raw === null ? undefined : Number(raw);
        if (zoneId !== undefined && !Number.isInteger(zoneId)) {
          return json({ error: "zoneId must be an integer" }, 400);
        }
        const { rows } = await pool.query<ZoneReading>(
          `SELECT z."ZoneId" AS zone_id, z."Name" AS zone_name, z."CropType" AS crop_type,
                  s."Temperature" AS temperature, s."Humidity" AS humidity, s."VPD" AS vpd,
                  s."SoilMoisture" AS soil_moisture, s."TimestampIST" AS timestamp_ist
             FROM "Zones" z
             LEFT JOIN LATERAL (
               SELECT "Temperature", "Humidity", "VPD", "SoilMoisture", "TimestampIST"
                 FROM "SensorLogs" sl
                WHERE sl."ZoneId" = z."ZoneId"
                ORDER BY sl."TimestampIST" DESC
                LIMIT 1
             ) s ON TRUE
            ${zoneId === undefined ? "" : 'WHERE z."ZoneId" = $1'}
            ORDER BY z."ZoneId"`,
          zoneId === undefined ? [] : [zoneId],
        );
        return json({ readings: rows, count: rows.length });
      }

      // POST /live-query — seal a live query the browser can subscribe to.
      // Authorization lives here: this app's telemetry is public read-only data,
      // so there is nothing user-specific to filter on.
      if (url.pathname === "/live-query" && request.method === "POST") {
        const body = (await request.json().catch(() => ({}))) as { zoneId?: unknown };
        const zoneId =
          typeof body.zoneId === "number" && Number.isInteger(body.zoneId)
            ? body.zoneId
            : undefined;
        const sealed = await realtime.seal({ query: latestReadings(zoneId) });
        return json(sealed);
      }

      return json(
        { service: "krishi-telemetry", endpoints: ["GET /readings", "POST /live-query"] },
        200,
      );
    } catch (error) {
      console.error("[krishi-telemetry]", error);
      return json({ error: "Internal error" }, 500);
    }
  },
};
