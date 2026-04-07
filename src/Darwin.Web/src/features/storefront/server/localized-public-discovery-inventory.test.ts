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
    "de-DE": "/cms/impressum",
    "en-US": "/en-US/cms/imprint",
  });
  assert.deepEqual(inventory.productAlternatesById.get("product-1"), {
    "de-DE": "/catalog/kaffee",
    "en-US": "/en-US/catalog/coffee",
  });
  assert.deepEqual(inventory.cmsSitemapEntries, [
    {
      path: "/cms/impressum",
      languageAlternates: {
        "de-DE": "/cms/impressum",
        "en-US": "/en-US/cms/imprint",
      },
    },
  ]);
  assert.deepEqual(inventory.productSitemapEntries, [
    {
      path: "/catalog/kaffee",
      languageAlternates: {
        "de-DE": "/catalog/kaffee",
        "en-US": "/en-US/catalog/coffee",
      },
    },
  ]);
});
