#!/usr/bin/env tsx
// ─────────────────────────────────────────────────────────────────────────────
// validate-selectors.ts
//
// Validates that every data-cy selector in registry.ts exists in the Angular
// app's HTML templates, and that every data-cy attribute in the templates has
// a matching entry in registry.ts.
//
// Usage:
//   npm run validate:selectors
//
// Output:
//   JSON report to stdout  (parsed by the /sync-selectors Claude skill)
//   Human summary to stderr
//
// Exit codes:
//   0 — all selectors in sync
//   1 — drift detected (added/removed/renamed selectors)
//   2 — configuration error (frontend dir not found)
// ─────────────────────────────────────────────────────────────────────────────

import * as fs   from 'fs';
import * as path from 'path';
import { AppSelectors } from '../selectors/registry';

// Path to Angular HTML templates, relative to the package.json directory
// (process.cwd() = Grants.AutoUI/ when run via npm scripts)
const FRONTEND_APP_DIR = path.resolve(
  process.cwd(),
  '../Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/src/app',
);

// ── Extract data-cy values from all Angular HTML templates ────────────────

interface HtmlMatch {
  value: string;
  file: string;
}

function extractFromHtml(dir: string): HtmlMatch[] {
  const results: HtmlMatch[] = [];

  function walk(currentDir: string): void {
    const entries = fs.readdirSync(currentDir, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = path.join(currentDir, entry.name);
      if (entry.isDirectory()) {
        walk(fullPath);
      } else if (entry.isFile() && entry.name.endsWith('.html')) {
        const content = fs.readFileSync(fullPath, 'utf-8');
        // Static bindings:  data-cy="value"
        const staticRe = /\bdata-cy="([^"]+)"/g;
        for (const match of content.matchAll(staticRe)) {
          results.push({ value: match[1], file: path.relative(process.cwd(), fullPath) });
        }
        // Angular attribute bindings:  [attr.data-cy]="'value'" or [attr.data-cy]="expr"
        // These are flagged separately as dynamic and not included in the static diff.
      }
    }
  }

  walk(dir);
  return results;
}

// ── Extract data-cy values from the registry (recursive object walk) ──────

interface RegistryExtract {
  staticValues: Set<string>;      // literal data-cy values
  dynamicPaths: string[];         // dot-paths to factory functions
}

function extractFromRegistry(obj: unknown, keyPath: string[] = []): RegistryExtract {
  const staticValues = new Set<string>();
  const dynamicPaths: string[] = [];

  function walk(current: unknown, path: string[]): void {
    if (typeof current === 'function') {
      dynamicPaths.push(path.join('.'));
      return;
    }
    if (typeof current === 'string') {
      // A single selector string may contain multiple data-cy references
      // (e.g. compound: '[data-cy="card-organization"] .orgbook-table')
      const re = /\[data-cy="([^"]+)"\]/g;
      for (const match of current.matchAll(re)) {
        staticValues.add(match[1]);
      }
      return;
    }
    if (typeof current === 'object' && current !== null) {
      for (const [key, value] of Object.entries(current)) {
        walk(value, [...path, key]);
      }
    }
  }

  walk(obj, keyPath);
  return { staticValues, dynamicPaths };
}

// ── Report types ──────────────────────────────────────────────────────────

interface SelectorReport {
  matched:          string[];
  onlyInApp:        Array<{ value: string; file: string }>;
  onlyInRegistry:   string[];
  dynamicSelectors: string[];
  summary: {
    total:   number;
    matched: number;
    drift:   number;
  };
}

// ── Main ──────────────────────────────────────────────────────────────────

function main(): void {
  if (!fs.existsSync(FRONTEND_APP_DIR)) {
    process.stderr.write(
      `\nERROR: Angular app directory not found:\n  ${FRONTEND_APP_DIR}\n\n` +
      `Run this script from the Grants.AutoUI package root.\n\n`,
    );
    process.exit(2);
  }

  const htmlMatches   = extractFromHtml(FRONTEND_APP_DIR);
  const registry      = extractFromRegistry(AppSelectors);

  // Deduplicated set of values found in HTML
  const htmlValues = new Set(htmlMatches.map(m => m.value));

  const matched:        string[]                              = [];
  const onlyInApp:      Array<{ value: string; file: string }> = [];
  const onlyInRegistry: string[]                              = [];

  // HTML → registry
  const seenInApp = new Set<string>();
  for (const { value, file } of htmlMatches) {
    if (seenInApp.has(value)) continue;  // deduplicate multi-occurrence values
    seenInApp.add(value);

    if (registry.staticValues.has(value)) {
      matched.push(value);
    } else {
      onlyInApp.push({ value, file });
    }
  }

  // Registry → HTML
  for (const value of registry.staticValues) {
    if (!htmlValues.has(value)) {
      onlyInRegistry.push(value);
    }
  }

  const report: SelectorReport = {
    matched,
    onlyInApp,
    onlyInRegistry,
    dynamicSelectors: registry.dynamicPaths,
    summary: {
      total:   matched.length + onlyInApp.length,
      matched: matched.length,
      drift:   onlyInApp.length + onlyInRegistry.length,
    },
  };

  // ── Structured JSON output (consumed by /sync-selectors skill) ────────────
  process.stdout.write(JSON.stringify(report, null, 2) + '\n');

  // ── Human-readable stderr summary ────────────────────────────────────────
  if (report.summary.drift === 0) {
    process.stderr.write(
      `\n✓ All ${matched.length} static data-cy selectors are in sync.\n` +
      `  Dynamic selectors (factory functions, not validated): ${registry.dynamicPaths.length}\n\n`,
    );
    process.exit(0);
  }

  process.stderr.write('\n✗ Selector drift detected:\n\n');

  if (onlyInApp.length > 0) {
    process.stderr.write(`  In app but NOT in registry (${onlyInApp.length}) — add these:\n`);
    for (const { value, file } of onlyInApp) {
      process.stderr.write(`    + "${value}"  (${file})\n`);
    }
    process.stderr.write('\n');
  }

  if (onlyInRegistry.length > 0) {
    process.stderr.write(`  In registry but NOT in app (${onlyInRegistry.length}) — removed or renamed:\n`);
    for (const value of onlyInRegistry) {
      process.stderr.write(`    - "${value}"\n`);
    }
    process.stderr.write('\n');
  }

  if (registry.dynamicPaths.length > 0) {
    process.stderr.write(
      `  Dynamic selectors skipped (${registry.dynamicPaths.length}): ` +
      registry.dynamicPaths.join(', ') + '\n\n',
    );
  }

  process.stderr.write(
    `  Run the /sync-selectors Claude skill to auto-fix the registry.\n\n`,
  );

  process.exit(1);
}

main();
