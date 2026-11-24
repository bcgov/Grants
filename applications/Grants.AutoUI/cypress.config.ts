import { defineConfig } from "cypress";
import * as fs from "node:fs";
import * as path from "node:path";

const ALLOWED_ENVS = new Set(["dev", "dev2", "test", "uat", "prod"]);

// https://docs.cypress.io/guides/references/configuration
function loadEnvConfig(env: string): Record<string, unknown> {
  if (!ALLOWED_ENVS.has(env)) {
    throw new Error(
      `Invalid ENV "${env}". Allowed values: ${Array.from(ALLOWED_ENVS).join(", ")}`,
    );
  }

  // Per-env file takes precedence so existing local setups keep working.
  const configPath = path.resolve(__dirname, `cypress/config/${env}.json`);
  if (fs.existsSync(configPath)) {
    return JSON.parse(fs.readFileSync(configPath, "utf-8"));
  }

  // Fallback: the combined file, same shape as the GRANTS_CYPRESS_CONFIG CI
  // secret — one CYPRESS_CONFIG_<ENV> key per environment.
  const combinedPath = path.resolve(__dirname, "cypress/config/grants-cypress-config.json");
  const key = `CYPRESS_CONFIG_${env.toUpperCase()}`;
  if (fs.existsSync(combinedPath)) {
    let entry = JSON.parse(fs.readFileSync(combinedPath, "utf-8"))[key];
    if (typeof entry === "string") {
      // Matches the workflow's jq `fromjson` handling of string-encoded values.
      entry = JSON.parse(entry);
    }
    if (entry) {
      return entry as Record<string, unknown>;
    }
  }

  throw new Error(
    `Environment config not found: ${configPath} or ${key} in ${combinedPath}`,
  );
}

export default defineConfig({
  allowCypressEnv: false,
  e2e: {
    setupNodeEvents(on, config) {
      const env = String(config.env["ENV"] || process.env.CYPRESS_ENV || "dev");
      const runtimeEnv = { ...config.env };
      const envConfig = loadEnvConfig(env);

      config.baseUrl = String(
        runtimeEnv.baseUrl || envConfig.baseUrl || config.baseUrl || "",
      );
      // Keep file config as defaults, but let runtime-provided values win.
      Object.assign(config.env, envConfig, runtimeEnv);
      config.expose = {
        ...config.expose,
        currentEnv: String(config.env["ENV"] || config.env["environment"] || "").toLowerCase(),
      };

      return config;
    },
    defaultCommandTimeout: 20000, // Time, in milliseconds, to wait until most DOM based commands are considered timed out.
    viewportWidth: 1440, // Default width in pixels.
    viewportHeight: 900, // Default height in pixels.
    chromeWebSecurity: false, // Chromium-based browser's Web Security for same-origin policy and insecure mixed content.
    testIsolation: true, // Ensure a clean browser context between test cases.
    // The number of times to retry a failing test.
    retries: {
      runMode: 1,
      openMode: 0,
    },
  },
});
