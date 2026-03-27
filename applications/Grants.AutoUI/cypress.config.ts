import { defineConfig } from "cypress";
import * as fs from "fs";
import * as path from "path";

// https://docs.cypress.io/guides/references/configuration
function loadEnvConfig(env: string): Record<string, string> {
  const configPath = path.resolve(__dirname, `cypress/config/${env}.json`);
  if (!fs.existsSync(configPath)) {
    throw new Error(`Environment config not found: ${configPath}`);
  }
  return JSON.parse(fs.readFileSync(configPath, "utf-8"));
}

export default defineConfig({
  e2e: {
    setupNodeEvents(on, config) {
      const env = config.env["ENV"] || process.env.CYPRESS_ENV || "dev";
      const envConfig = loadEnvConfig(env);

      config.baseUrl = envConfig.baseUrl;
      Object.assign(config.env, envConfig);

      return config;
    },
    defaultCommandTimeout: 20000, // Time, in milliseconds, to wait until most DOM based commands are considered timed out.
    viewportWidth: 1440, // Default width in pixels.
    viewportHeight: 900, // Default height in pixels.
    chromeWebSecurity: false, // Chromium-based browser's Web Security for same-origin policy and insecure mixed content.
    testIsolation: true, // Ensure a clean browser context between test cases.
    // The number of times to retry a failing test.
    retries: {
      runMode: 3,
      openMode: 0,
    },
  },
});
