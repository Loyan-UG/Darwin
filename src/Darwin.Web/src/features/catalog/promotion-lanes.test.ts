import assert from "node:assert/strict";
import test from "node:test";
import { summarizeCatalogPromotionLanes } from "@/features/catalog/promotion-lanes";
import type { PublicProductSummary } from "@/features/catalog/types";

const products: PublicProductSummary[] = [
  {
    id: "hero",
    name: "Hero cereal",
    slug: "hero-cereal",
    currency: "EUR",
    priceMinor: 500,
    compareAtPriceMinor: 800,
  },
  {
    id: "value",
    name: "Value pasta",
    slug: "value-pasta",
    currency: "EUR",
    priceMinor: 700,
    compareAtPriceMinor: 900,
  },
  {
    id: "offer",
    name: "Offer chips",
    slug: "offer-chips",
    currency: "EUR",
    priceMinor: 800,
    compareAtPriceMinor: 850,
  },
  {
    id: "base",
    name: "Base water",
    slug: "base-water",
    currency: "EUR",
    priceMinor: 200,
  },
];

test("summarizeCatalogPromotionLanes groups visible products into live promotion lanes", () => {
  const summary = summarizeCatalogPromotionLanes(products);

  assert.deepEqual(
    summary.map((entry) => [entry.lane, entry.count, entry.anchorProduct?.id ?? null]),
    [
      ["hero-offers", 1, "hero"],
      ["value-offers", 2, "hero"],
      ["live-offers", 3, "hero"],
      ["base-assortment", 1, "base"],
    ],
  );
});
