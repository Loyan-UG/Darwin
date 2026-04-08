import assert from "node:assert/strict";
import test from "node:test";
import { buildStorefrontSpotlightSelections } from "@/features/storefront/storefront-spotlight";

test("buildStorefrontSpotlightSelections keeps one shared spotlight and campaign model", () => {
  const result = buildStorefrontSpotlightSelections({
    cmsPages: [
      { id: "page-1", slug: "eins", title: "Eins", metaTitle: "Eins", metaDescription: "Desc" },
      { id: "page-2", slug: "zwei", title: "Zwei", metaTitle: "Zwei", metaDescription: "Desc 2" },
    ],
    categories: [
      { id: "cat-1", slug: "a", name: "A", description: "Cat A" },
      { id: "cat-2", slug: "b", name: "B", description: "Cat B" },
      { id: "cat-3", slug: "c", name: "C", description: "Cat C" },
    ],
    products: [
      {
        id: "prod-1",
        slug: "steady",
        name: "Steady",
        shortDescription: null,
        priceMinor: 100,
        compareAtPriceMinor: null,
        currency: "EUR",
        primaryImageUrl: null,
      },
      {
        id: "prod-2",
        slug: "hero",
        name: "Hero",
        shortDescription: null,
        priceMinor: 100,
        compareAtPriceMinor: 200,
        currency: "EUR",
        primaryImageUrl: null,
      },
      {
        id: "prod-3",
        slug: "value",
        name: "Value",
        shortDescription: null,
        priceMinor: 100,
        compareAtPriceMinor: 150,
        currency: "EUR",
        primaryImageUrl: null,
      },
    ],
    categoryCampaignCount: 2,
    productCampaignCount: 2,
    offerBoardCount: 2,
  });

  assert.equal(result.spotlightPage?.id, "page-1");
  assert.equal(result.spotlightCategory?.id, "cat-1");
  assert.equal(result.spotlightProduct?.id, "prod-2");
  assert.deepEqual(
    result.offerBoardProducts.map((product) => product.id),
    ["prod-2", "prod-3"],
  );
  assert.deepEqual(
    result.campaignCategories.map((category) => category.id),
    ["cat-1", "cat-2"],
  );
  assert.deepEqual(
    result.campaignProducts.map((product) => product.id),
    ["prod-2", "prod-3"],
  );
});
