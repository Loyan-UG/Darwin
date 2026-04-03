import test from "node:test";
import assert from "node:assert/strict";
import { availableThemes, resolveTheme } from "@/themes/registry";

test("resolveTheme returns the configured runtime theme when available", () => {
  assert.equal(resolveTheme("atelier").id, "atelier");
  assert.equal(resolveTheme("harbor").id, "harbor");
});

test("resolveTheme falls back to the default theme for unknown ids", () => {
  assert.equal(resolveTheme("unknown-theme").id, availableThemes[0]?.id);
});
