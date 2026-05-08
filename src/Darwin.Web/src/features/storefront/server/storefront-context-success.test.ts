import assert from "node:assert/strict";
import test from "node:test";
import { buildPublicStorefrontSuccessContext } from "@/features/storefront/server/get-public-storefront-context";
import { buildStorefrontContinuationSuccessContext } from "@/features/storefront/server/get-storefront-continuation-context";

function createProduct(
  slug: string,
  priceMinor: number,
  compareAtPriceMinor: number | null,
) {
  return {
    id: slug,
    slug,
    name: slug,
    shortDescription: null,
    priceMinor,
    compareAtPriceMinor,
    currency: "EUR",
    primaryImageUrl: null,
  };
}

test("buildStorefrontContinuationSuccessContext keeps continuation summary and shared footprint aligned", () => {
  assert.deepEqual(
    buildStorefrontContinuationSuccessContext({
      cmsPagesStatus: "ok",
      cmsPages: [{ id: "page-1" }, { id: "page-2" }],
      categoriesStatus: "degraded",
      categories: [{ id: "cat-1" }],
      productsStatus: "ok",
      products: [
        createProduct("hero-1", 500, 900),
        createProduct("value-1", 400, 700),
        createProduct("base-1", 300, null),
      ],
    }),
    {
      cmsStatus: "ok",
      cmsCount: 2,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 2,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:2|value:2|live:2|base:1",
      sharedContextFootprint: "cms:ok|categories:degraded|products:ok",
    },
  );
});

test("buildPublicStorefrontSuccessContext keeps public storefront summary and shared footprint aligned", () => {
  assert.deepEqual(
    buildPublicStorefrontSuccessContext({
      cmsPagesResult: { status: "ok", data: null },
      cmsPagesStatus: "ok",
      cmsPages: [{ id: "page-1" }],
      categoriesResult: { status: "warning", data: null },
      categoriesStatus: "warning",
      categories: [{ id: "cat-1" }],
      productsResult: { status: "ok", data: null },
      productsStatus: "ok",
      products: [
        createProduct("hero-1", 500, 900),
        createProduct("value-1", 400, 700),
        createProduct("base-1", 300, null),
      ],
      storefrontCart: null,
      storefrontCartStatus: "not-found",
      cartSnapshots: [],
      cartLinkedProductSlugs: ["hero-1"],
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "warning",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 2,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:2|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      sharedContextFootprint: "cms:ok|categories:warning|products:ok|cart:not-found",
    },
  );
});
