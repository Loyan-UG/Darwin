import test from "node:test";
import assert from "node:assert/strict";
import { formatDateTime, formatMoney } from "@/lib/formatting";

test("formatMoney uses the configured default culture when locale is missing", () => {
  const previousDefaultCulture = process.env.DARWIN_WEB_CULTURE;
  const previousSupportedCultures = process.env.DARWIN_WEB_SUPPORTED_CULTURES;
  process.env.DARWIN_WEB_CULTURE = "en-US";
  process.env.DARWIN_WEB_SUPPORTED_CULTURES = "de-DE,en-US";

  try {
    const formatted = formatMoney(12345, "EUR");
    assert.match(formatted, /€|EUR/);
    assert.match(formatted, /123\.45/);
  } finally {
    if (previousDefaultCulture === undefined) {
      delete process.env.DARWIN_WEB_CULTURE;
    } else {
      process.env.DARWIN_WEB_CULTURE = previousDefaultCulture;
    }

    if (previousSupportedCultures === undefined) {
      delete process.env.DARWIN_WEB_SUPPORTED_CULTURES;
    } else {
      process.env.DARWIN_WEB_SUPPORTED_CULTURES = previousSupportedCultures;
    }
  }
});

test("formatMoney falls back to a supported language variant before the default culture", () => {
  const previousDefaultCulture = process.env.DARWIN_WEB_CULTURE;
  const previousSupportedCultures = process.env.DARWIN_WEB_SUPPORTED_CULTURES;
  process.env.DARWIN_WEB_CULTURE = "de-DE";
  process.env.DARWIN_WEB_SUPPORTED_CULTURES = "de-DE,en-US";

  try {
    const formatted = formatMoney(12345, "EUR", "en-GB");
    assert.match(formatted, /€|EUR/);
    assert.match(formatted, /123\.45/);
  } finally {
    if (previousDefaultCulture === undefined) {
      delete process.env.DARWIN_WEB_CULTURE;
    } else {
      process.env.DARWIN_WEB_CULTURE = previousDefaultCulture;
    }

    if (previousSupportedCultures === undefined) {
      delete process.env.DARWIN_WEB_SUPPORTED_CULTURES;
    } else {
      process.env.DARWIN_WEB_SUPPORTED_CULTURES = previousSupportedCultures;
    }
  }
});

test("formatDateTime falls back to the configured default culture when locale is unsupported", () => {
  const previousDefaultCulture = process.env.DARWIN_WEB_CULTURE;
  const previousSupportedCultures = process.env.DARWIN_WEB_SUPPORTED_CULTURES;
  process.env.DARWIN_WEB_CULTURE = "de-DE";
  process.env.DARWIN_WEB_SUPPORTED_CULTURES = "de-DE,en-US";

  try {
    const unsupported = formatDateTime("2026-04-20T08:30:00Z", "fr-FR");
    const defaultCulture = formatDateTime("2026-04-20T08:30:00Z", "de-DE");
    assert.equal(unsupported, defaultCulture);
  } finally {
    if (previousDefaultCulture === undefined) {
      delete process.env.DARWIN_WEB_CULTURE;
    } else {
      process.env.DARWIN_WEB_CULTURE = previousDefaultCulture;
    }

    if (previousSupportedCultures === undefined) {
      delete process.env.DARWIN_WEB_SUPPORTED_CULTURES;
    } else {
      process.env.DARWIN_WEB_SUPPORTED_CULTURES = previousSupportedCultures;
    }
  }
});
