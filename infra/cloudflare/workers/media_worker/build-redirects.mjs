#!/usr/bin/env node
/**
 * Build script for the media worker's REDIRECTS map.
 *
 * Reads ../../../../scripts/rename-map.json and writes redirects.generated.js
 * with a const REDIRECTS = { decodedLegacyPath: canonicalPath, ... } map.
 *
 * The decode step matters: the worker observes pathnames that the runtime has
 * already URL-decoded (e.g. "%27" → "'", "+" stays as "+"), but legacy URLs
 * in articles/*.json were percent-encoded with `+` for spaces. We normalise to
 * what the worker actually receives.
 *
 * Run before wrangler deploy. The generated file is gitignored — the
 * checked-in source of truth is scripts/rename-map.json.
 */

import { readFile, writeFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { dirname, resolve } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const MAP_PATH = resolve(__dirname, "../../../../scripts/rename-map.json");
const OUTPUT_PATH = resolve(__dirname, "redirects.generated.js");

function decodeLegacy(s) {
  // The worker observes URL.pathname which is already %-decoded for the path
  // segment. Source legacy URLs use `+` for spaces (URL query convention) but
  // by the time they reach the worker as bookmarks, browsers preserve `+` as
  // literal `+`. Decode %-escapes only; leave `+` as-is.
  return s.replace(/%([0-9A-Fa-f]{2})/g, (_, h) => String.fromCharCode(parseInt(h, 16)));
}

const map = JSON.parse(await readFile(MAP_PATH, "utf8"));
const pdfMoves = map.moves.filter(m => m.kind === "pdf" || m.kind === "extra");

const redirects = {};
for (const { legacy, canonical } of pdfMoves) {
  const key = decodeLegacy(legacy);
  redirects[key] = canonical;
  // Also accept the `+` → space variant: some old bookmarks may have spaces
  // already decoded. Browsers may also send the literal `+` form depending on
  // how the URL was constructed. Add both to cover the cases that actually
  // appear in the wild.
  const withSpaces = key.replace(/\+/g, " ");
  if (withSpaces !== key) {
    redirects[withSpaces] = canonical;
  }
}

const banner = `// AUTO-GENERATED from scripts/rename-map.json — do not edit.
// Regenerate via: node build-redirects.mjs
// Generated at: ${new Date().toISOString()}
// Source: ${map.source}
// Entry count: ${Object.keys(redirects).length}
`;

const body = `export const REDIRECTS = ${JSON.stringify(redirects, null, 2)};\n`;

await writeFile(OUTPUT_PATH, banner + body);
console.log(`Wrote ${OUTPUT_PATH}: ${Object.keys(redirects).length} redirects`);
