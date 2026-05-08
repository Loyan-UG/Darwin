import assert from "node:assert/strict";
import test from "node:test";
import { projectLocalizedPublicDiscoveryInventory } from "@/features/storefront/server/localized-public-discovery-projections";

test("projectLocalizedPublicDiscoveryInventory precomputes alternates and sitemap entries from one localized snapshot", () => {
  const inventory = projectLocalizedPublicDiscoveryInventory([
    {
      culture: "de-DE",
      pages: [{ id: "page-1", slug: "impressum" }],
      products: [{ id: "product-1", slug: "kaffee" }],
    },
    {
      culture: "en-US",
      pages: [{ id: "page-1", slug: "imprint" }],
      products: [{ id: "product-1", slug: "coffee" }],
    },
  ]);

  assert.deepEqual(inventory.pages, [
    {
      culture: "de-DE",
      items: [{ id: "page-1", slug: "impressum" }],
    },
    {
      culture: "en-US",
      items: [{ id: "page-1", slug: "imprint" }],
    },
  ]);
  assert.deepEqual(inventory.products, [
    {
      culture: "de-DE",
      items: [{ id: "product-1", slug: "kaffee" }],
    },
    {
      culture: "en-US",
      items: [{ id: "product-1", slug: "coffee" }],
    },
  ]);
  assert.deepEqual(inventory.pageAlternatesById.get("page-1"), {
    "de-DE": "/page/impressum",
    "en-US": "/page/imprint",
  });
  assert.deepEqual(inventory.productAlternatesById.get("product-1"), {
    "de-DE": "/catalog/kaffee",
    "en-US": "/catalog/coffee",
  });
  assert.deepEqual(inventory.cmsSitemapEntries, [
    {
      path: "/page/impressum",
      languageAlternates: {
        "de-DE": "/page/impressum",
        "en-US": "/page/imprint",
      },
    },
  ]);
  assert.deepEqual(inventory.productSitemapEntries, [
    {
      path: "/catalog/kaffee",
      languageAlternates: {
        "de-DE": "/catalog/kaffee",
        "en-US": "/catalog/coffee",
      },
    },
  ]);
});

test("projectLocalizedPublicDiscoveryInventory skips invalid items and keeps localized alternates canonical", () => {
  const inventory = projectLocalizedPublicDiscoveryInventory([
    {
      culture: "en-US",
      pages: [
        { id: "page-1", slug: "imprint" },
        { id: "", slug: "ignored-empty-id" },
        { id: "page-missing-slug", slug: "" },
      ],
      products: [
        { id: "product-1", slug: "coffee" },
        { id: "product-missing-slug", slug: "" },
      ],
    },
    {
      culture: "de-DE",
      pages: [{ id: "page-1", slug: "impressum" }],
      products: [
        { id: "product-1", slug: "kaffee" },
        { id: "", slug: "ignored-empty-id" },
      ],
    },
  ]);

  assert.deepEqual(inventory.pageAlternatesById.get("page-1"), {
    "de-DE": "/page/impressum",
    "en-US": "/page/imprint",
  });
  assert.equal(inventory.pageAlternatesById.has("page-missing-slug"), false);
  assert.deepEqual(inventory.productAlternatesById.get("product-1"), {
    "de-DE": "/catalog/kaffee",
    "en-US": "/catalog/coffee",
  });
  assert.equal(
    inventory.productAlternatesById.has("product-missing-slug"),
    false,
  );
  assert.deepEqual(inventory.cmsSitemapEntries, [
    {
      path: "/page/impressum",
      languageAlternates: {
        "de-DE": "/page/impressum",
        "en-US": "/page/imprint",
      },
    },
  ]);
  assert.deepEqual(inventory.productSitemapEntries, [
    {
      path: "/catalog/kaffee",
      languageAlternates: {
        "de-DE": "/catalog/kaffee",
        "en-US": "/catalog/coffee",
      },
    },
  ]);
});
