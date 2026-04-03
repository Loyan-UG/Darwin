import test from "node:test";
import assert from "node:assert/strict";
import type { PublicProductSummary } from "@/features/catalog/types";
import {
  getProductOpportunityCampaign,
  getProductOpportunityCampaignLabel,
  getProductSavingsPercent,
  sortProductsByOpportunity,
} from "@/features/catalog/merchandising";

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

test("getProductSavingsPercent returns null when compare-at is missing or not stronger", () => {
  assert.equal(
    getProductSavingsPercent(createProduct({ compareAtPriceMinor: null })),
    null,
  );
  assert.equal(
    getProductSavingsPercent(
      createProduct({ priceMinor: 1000, compareAtPriceMinor: 1000 }),
    ),
    null,
  );
});

test("getProductOpportunityCampaign classifies visible offers into business tiers", () => {
  assert.equal(
    getProductOpportunityCampaign(
      createProduct({ priceMinor: 700, compareAtPriceMinor: 1000 }),
    ),
    "hero-offer",
  );
  assert.equal(
    getProductOpportunityCampaign(
      createProduct({ priceMinor: 850, compareAtPriceMinor: 1000 }),
    ),
    "value-offer",
  );
  assert.equal(
    getProductOpportunityCampaign(
      createProduct({ priceMinor: 920, compareAtPriceMinor: 1000 }),
    ),
    "price-drop",
  );
  assert.equal(
    getProductOpportunityCampaign(createProduct({ compareAtPriceMinor: null })),
    "steady-pick",
  );
});

test("sortProductsByOpportunity keeps the strongest savings signal first", () => {
  const sorted = sortProductsByOpportunity([
    createProduct({
      id: "steady",
      slug: "steady",
      priceMinor: 1000,
      compareAtPriceMinor: null,
    }),
    createProduct({
      id: "value",
      slug: "value",
      priceMinor: 850,
      compareAtPriceMinor: 1000,
    }),
    createProduct({
      id: "hero",
      slug: "hero",
      priceMinor: 700,
      compareAtPriceMinor: 1000,
    }),
  ]);

  assert.deepEqual(
    sorted.map((product) => product.id),
    ["hero", "value", "steady"],
  );
});

test("getProductOpportunityCampaignLabel maps each tier to the provided merchandising copy", () => {
  const labels = {
    heroOffer: "Hero offer",
    valueOffer: "Value offer",
    priceDrop: "Price drop",
    steadyPick: "Steady pick",
  };

  assert.equal(
    getProductOpportunityCampaignLabel("hero-offer", labels),
    "Hero offer",
  );
  assert.equal(
    getProductOpportunityCampaignLabel("value-offer", labels),
    "Value offer",
  );
  assert.equal(
    getProductOpportunityCampaignLabel("price-drop", labels),
    "Price drop",
  );
  assert.equal(
    getProductOpportunityCampaignLabel("steady-pick", labels),
    "Steady pick",
  );
});
