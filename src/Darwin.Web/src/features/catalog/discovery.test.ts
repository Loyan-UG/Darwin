import test from "node:test";
import assert from "node:assert/strict";
import type { PublicProductSummary } from "@/features/catalog/types";
import {
  filterCatalogVisibleProducts,
  getCatalogSavingsAmount,
  readCatalogVisibleState,
  sortCatalogVisibleProducts,
} from "@/features/catalog/discovery";

function createProduct(
  overrides: Partial<PublicProductSummary>,
): PublicProductSummary {
  return {
    id: overrides.id ?? "product-1",
    name: overrides.name ?? "Product",
    slug: overrides.slug ?? "product",
    currency: overrides.currency ?? "EUR",
    priceMinor: overrides.priceMinor ?? 1000,
    compareAtPriceMinor: overrides.compareAtPriceMinor ?? null,
    shortDescription: overrides.shortDescription ?? null,
    primaryImageUrl: overrides.primaryImageUrl ?? null,
  };
}

test("readCatalogVisibleState keeps only supported catalog lens values", () => {
  assert.equal(readCatalogVisibleState("offers"), "offers");
  assert.equal(readCatalogVisibleState("base"), "base");
  assert.equal(readCatalogVisibleState("unknown"), "all");
});

test("getCatalogSavingsAmount returns only positive compare-at savings", () => {
  assert.equal(
    getCatalogSavingsAmount(
      createProduct({ priceMinor: 700, compareAtPriceMinor: 1000 }),
    ),
    300,
  );
  assert.equal(
    getCatalogSavingsAmount(
      createProduct({ priceMinor: 1000, compareAtPriceMinor: 1000 }),
    ),
    0,
  );
});

test("filterCatalogVisibleProducts applies query and visible-state lens together", () => {
  const products = [
    createProduct({
      id: "offer",
      name: "Spring Offer",
      slug: "spring-offer",
      priceMinor: 700,
      compareAtPriceMinor: 1000,
    }),
    createProduct({
      id: "base",
      name: "Base Product",
      slug: "base-product",
      shortDescription: "steady assortment",
      compareAtPriceMinor: null,
    }),
  ];

  assert.deepEqual(
    filterCatalogVisibleProducts(products, "offers").map((product) => product.id),
    ["offer"],
  );
  assert.deepEqual(
    filterCatalogVisibleProducts(products, "base").map((product) => product.id),
    ["base"],
  );
  assert.deepEqual(
    filterCatalogVisibleProducts(products, "all", "steady").map(
      (product) => product.id,
    ),
    ["base"],
  );
});

test("sortCatalogVisibleProducts prioritizes offer-first or base-first review windows", () => {
  const products = [
    createProduct({
      id: "offer-b",
      name: "Bravo Offer",
      priceMinor: 700,
      compareAtPriceMinor: 1000,
    }),
    createProduct({
      id: "base-a",
      name: "Alpha Base",
      compareAtPriceMinor: null,
    }),
    createProduct({
      id: "offer-c",
      name: "Charlie Offer",
      priceMinor: 800,
      compareAtPriceMinor: 1000,
    }),
  ];

  assert.deepEqual(
    sortCatalogVisibleProducts(products, "offers-first").map(
      (product) => product.id,
    ),
    ["offer-b", "offer-c", "base-a"],
  );
  assert.deepEqual(
    sortCatalogVisibleProducts(products, "base-first").map(
      (product) => product.id,
    ),
    ["base-a", "offer-b", "offer-c"],
  );
});
