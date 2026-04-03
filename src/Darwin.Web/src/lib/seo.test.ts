import test from "node:test";
import assert from "node:assert/strict";
import { buildSeoMetadata } from "@/lib/seo";

test("buildSeoMetadata prefers explicit language alternates when provided", () => {
  const metadata = buildSeoMetadata({
    culture: "de-DE",
    title: "Product",
    path: "/catalog/prod",
    languageAlternates: {
      "x-default": "/catalog/prod",
      "de-DE": "/catalog/prod",
      "en-US": "/en-US/catalog/product",
    },
  });

  assert.deepEqual(metadata.alternates?.languages, {
    "x-default": "/catalog/prod",
    "de-DE": "/catalog/prod",
    "en-US": "/en-US/catalog/product",
  });
});

test("buildSeoMetadata derives x-default from the configured default culture when explicit alternates omit it", () => {
  const metadata = buildSeoMetadata({
    culture: "en-US",
    title: "Product",
    path: "/catalog/product",
    languageAlternates: {
      "de-DE": "/catalog/produkt",
      "en-US": "/en-US/catalog/product",
    },
  });

  assert.deepEqual(metadata.alternates?.languages, {
    "x-default": "/catalog/produkt",
    "de-DE": "/catalog/produkt",
    "en-US": "/en-US/catalog/product",
  });
});
