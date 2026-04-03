import test from "node:test";
import assert from "node:assert/strict";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

const originalEnv = {
  DARWIN_WEB_THEME: process.env.DARWIN_WEB_THEME,
  DARWIN_WEB_CULTURE: process.env.DARWIN_WEB_CULTURE,
  DARWIN_WEB_SUPPORTED_CULTURES: process.env.DARWIN_WEB_SUPPORTED_CULTURES,
};

test.after(() => {
  process.env.DARWIN_WEB_THEME = originalEnv.DARWIN_WEB_THEME;
  process.env.DARWIN_WEB_CULTURE = originalEnv.DARWIN_WEB_CULTURE;
  process.env.DARWIN_WEB_SUPPORTED_CULTURES =
    originalEnv.DARWIN_WEB_SUPPORTED_CULTURES;
});

test("getSiteRuntimeConfig accepts every registered runtime theme", () => {
  process.env.DARWIN_WEB_THEME = "solstice";

  assert.equal(getSiteRuntimeConfig().theme, "solstice");

  process.env.DARWIN_WEB_THEME = "noir";

  assert.equal(getSiteRuntimeConfig().theme, "noir");
});

test("getSiteRuntimeConfig falls back to the default registered theme for unknown values", () => {
  process.env.DARWIN_WEB_THEME = "unknown-theme";

  assert.equal(getSiteRuntimeConfig().theme, "grocer");
});

test("getSiteRuntimeConfig keeps the default culture inside the supported set", () => {
  process.env.DARWIN_WEB_SUPPORTED_CULTURES = "de-DE,en-US";
  process.env.DARWIN_WEB_CULTURE = "fr-FR";

  const config = getSiteRuntimeConfig();

  assert.deepEqual(config.supportedCultures, ["de-DE", "en-US"]);
  assert.equal(config.defaultCulture, "de-DE");
});
