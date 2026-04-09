import assert from "node:assert/strict";
import test from "node:test";
import { canonicalizeLanguageAlternates } from "@/lib/localized-alternates";

test("canonicalizeLanguageAlternates keeps x-default first and sorts cultures", () => {
  assert.deepEqual(
    canonicalizeLanguageAlternates({
      "en-US": "/en-US/catalog/product",
      "x-default": "/catalog/produkt",
      "de-DE": "/catalog/produkt",
    }),
    {
      "x-default": "/catalog/produkt",
      "de-DE": "/catalog/produkt",
      "en-US": "/en-US/catalog/product",
    },
  );
});

test("canonicalizeLanguageAlternates drops empty paths and returns undefined when empty", () => {
  assert.equal(
    canonicalizeLanguageAlternates({
      "de-DE": "",
    }),
    undefined,
  );
});
