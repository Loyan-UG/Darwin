import test from "node:test";
import assert from "node:assert/strict";
import type { PublicProductSummary } from "@/features/catalog/types";
import {
  filterProductsByExcludedCatalogPaths,
  filterProductsByPurchasedNames,
} from "@/features/storefront/shopping-follow-up";

function createProduct(
  id: string,
  name: string,
  slug: string,
): PublicProductSummary {
  return {
    id,
    name,
    slug,
    currency: "EUR",
    priceMinor: 1200,
  };
}

test("filterProductsByExcludedCatalogPaths removes products already linked to the active cart", () => {
  const products = [
    createProduct("1", "Apple Box", "apple-box"),
    createProduct("2", "Berry Mix", "berry-mix"),
    createProduct("3", "Cocoa Bar", "cocoa-bar"),
  ];

  const result = filterProductsByExcludedCatalogPaths(products, [
    "/catalog/berry-mix",
  ]);

  assert.deepEqual(
    result.map((product) => product.slug),
    ["apple-box", "cocoa-bar"],
  );
});

test("filterProductsByPurchasedNames removes purchased products case-insensitively", () => {
  const products = [
    createProduct("1", "Apple Box", "apple-box"),
    createProduct("2", "Berry Mix", "berry-mix"),
    createProduct("3", "Cocoa Bar", "cocoa-bar"),
  ];

  const result = filterProductsByPurchasedNames(products, [" berry mix ", "APPLE BOX"]);

  assert.deepEqual(result.map((product) => product.slug), ["cocoa-bar"]);
});
