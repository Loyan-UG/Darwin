import test from "node:test";
import assert from "node:assert/strict";
import type { PublicProductSummary } from "@/features/catalog/types";
import {
  buildCatalogVisibleWindow,
  filterCatalogVisibleProducts,
  getCatalogQualityTargets,
  getCatalogSavingsPercent,
  getCatalogSavingsAmount,
  hasPrimaryImage,
  readCatalogMediaState,
  readCatalogSavingsBand,
  readCatalogVisibleSort,
  readCatalogVisibleState,
  summarizeCatalogFacets,
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

test("readCatalogVisibleSort keeps only supported catalog sort values", () => {
  assert.equal(readCatalogVisibleSort("offers-first"), "offers-first");
  assert.equal(readCatalogVisibleSort("base-first"), "base-first");
  assert.equal(readCatalogVisibleSort("price-desc"), "price-desc");
  assert.equal(readCatalogVisibleSort("unknown"), "featured");
});

test("catalog facet readers keep only supported facet values", () => {
  assert.equal(readCatalogMediaState("with-image"), "with-image");
  assert.equal(readCatalogMediaState("invalid"), "all");
  assert.equal(readCatalogSavingsBand("hero"), "hero");
  assert.equal(readCatalogSavingsBand("invalid"), "all");
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

test("catalog merchandising helpers expose image presence and savings percent", () => {
  const product = createProduct({
    primaryImageUrl: "/media/product.png",
    priceMinor: 700,
    compareAtPriceMinor: 1000,
  });

  assert.equal(hasPrimaryImage(product), true);
  assert.equal(getCatalogSavingsPercent(product), 30);
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
  assert.deepEqual(
    filterCatalogVisibleProducts(
      products,
      "all",
      undefined,
      "missing-image",
      "all",
    ).map((product) => product.id),
    ["offer", "base"],
  );
});

test("sortCatalogVisibleProducts prioritizes offer-first or base-first browse windows", () => {
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

test("buildCatalogVisibleWindow paginates the full matching assortment after filters and sort", () => {
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

  const window = buildCatalogVisibleWindow(products, {
    page: 2,
    pageSize: 1,
    visibleState: "offers",
    visibleSort: "price-desc",
  });

  assert.equal(window.total, 2);
  assert.equal(window.totalPages, 2);
  assert.equal(window.currentPage, 2);
  assert.deepEqual(window.items.map((product) => product.id), ["offer-b"]);
});

test("summarizeCatalogFacets counts filterable browse states from the matching set", () => {
  const products = [
    createProduct({
      id: "hero",
      priceMinor: 700,
      compareAtPriceMinor: 1000,
      primaryImageUrl: "/media/hero.png",
    }),
    createProduct({
      id: "value",
      priceMinor: 900,
      compareAtPriceMinor: 1000,
      primaryImageUrl: null,
    }),
    createProduct({
      id: "base",
      compareAtPriceMinor: null,
      primaryImageUrl: "/media/base.png",
    }),
  ];

  assert.deepEqual(summarizeCatalogFacets(products), {
    totalCount: 3,
    offerCount: 2,
    baseCount: 1,
    withImageCount: 2,
    missingImageCount: 1,
    valueOfferCount: 2,
    heroOfferCount: 1,
  });
});

test("getCatalogQualityTargets prioritizes missing images before savings-led quality targets", () => {
  const products = [
    createProduct({
      id: "missing-image",
      name: "Missing Image",
      primaryImageUrl: null,
      compareAtPriceMinor: null,
    }),
    createProduct({
      id: "strong-offer",
      name: "Strong Offer",
      primaryImageUrl: "/media/offer.png",
      priceMinor: 700,
      compareAtPriceMinor: 1000,
    }),
    createProduct({
      id: "base",
      name: "Base Product",
      primaryImageUrl: "/media/base.png",
      compareAtPriceMinor: null,
    }),
  ];

  const targets = getCatalogQualityTargets(products);

  assert.deepEqual(targets.map((target) => target.product.id), [
    "missing-image",
    "strong-offer",
    "base",
  ]);
  assert.equal(targets[0]?.missingImage, true);
  assert.equal(targets[1]?.savingsAmount, 300);
});
