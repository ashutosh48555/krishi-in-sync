import { defineConfig } from "@neon/config/v1";

export default defineConfig({
  // Declare your Neon services here
  preview: {
    realtime: true,
  },
  auth: false,
  functions: {
    telemetry: {
      name: "Krishi Telemetry API",
      source: "./telemetry.ts",
      // Realtime secret is not auto-injected into Functions; pass it at deploy time from .env.local.
      env: {
        NEON_REALTIME_SECRET: { value: process.env.NEON_REALTIME_SECRET, required: true },
        NEON_REALTIME_URL: { value: process.env.NEON_REALTIME_URL, required: true },
      },
    },
  },
  // Branch policy: per-branch tuning
  branch: (branch) => {
    if (branch.isDefault) {
      // Default branch: no overrides, uses project defaults
      return {};
    }
    if (!branch.exists) {
      // New non-default branches: auto-expire
      // Run `neon checkout <name>` to create a new branch with these settings
      return { ttl: "7d" };
    }
    // Existing branch: no changes
    return {};
  },
});
