import test from "node:test";
import assert from "node:assert/strict";
import { mergePublicStorefrontContext } from "@/features/storefront/public-storefront-context";
import { summarizeStorefrontContinuationHealth } from "@/lib/route-health";

test("mergePublicStorefrontContext keeps content and browse continuity together with live cart state", () => {
  const continuationContext = {
    cmsPagesResult: { status: "ok", data: { items: [{ id: "page-1" }] } },
    cmsPages: [{ id: "page-1" }],
    cmsPagesStatus: "ok",
    categoriesResult: { status: "ok", data: { items: [{ id: "cat-1" }] } },
    categories: [{ id: "cat-1" }],
    categoriesStatus: "ok",
    productsResult: { status: "ok", data: { items: [{ id: "product-1" }] } },
    products: [{ id: "product-1" }],
    productsStatus: "ok",
  };
  const shoppingContext = {
    cartResult: {
      data: { id: "cart-1", items: [{ id: "line-1" }] },
      status: "ok",
    },
    cartSnapshots: [{ variantId: "variant-1" }],
    cartLinkedProductSlugs: ["product-1"],
  };

  const result = mergePublicStorefrontContext(
    continuationContext,
    shoppingContext,
  );

  assert.equal(result.cmsPagesStatus, "ok");
  assert.equal(result.categoriesStatus, "ok");
  assert.equal(result.productsStatus, "ok");
  assert.deepEqual(result.storefrontCart, shoppingContext.cartResult.data);
  assert.equal(result.storefrontCartStatus, "ok");
  assert.deepEqual(result.cartSnapshots, shoppingContext.cartSnapshots);
  assert.deepEqual(
    result.cartLinkedProductSlugs,
    shoppingContext.cartLinkedProductSlugs,
  );
});

test("mergePublicStorefrontContext preserves degraded cart continuity without dropping public discovery state", () => {
  const continuationContext = {
    cmsPagesResult: { status: "ok", data: { items: [] } },
    cmsPages: [],
    cmsPagesStatus: "ok",
    categoriesResult: { status: "warning", data: { items: [] } },
    categories: [],
    categoriesStatus: "warning",
    productsResult: { status: "degraded", data: { items: [] } },
    products: [],
    productsStatus: "degraded",
  };
  const shoppingContext = {
    cartResult: {
      data: null,
      status: "not-found",
    },
    cartSnapshots: [],
    cartLinkedProductSlugs: [],
  };

  const result = mergePublicStorefrontContext(
    continuationContext,
    shoppingContext,
  );

  assert.equal(result.categoriesStatus, "warning");
  assert.equal(result.productsStatus, "degraded");
  assert.equal(result.storefrontCart, null);
  assert.equal(result.storefrontCartStatus, "not-found");
  assert.deepEqual(result.cartSnapshots, []);
});

test("summarizeStorefrontContinuationHealth exposes canonical continuation counts and statuses", () => {
  const summary = summarizeStorefrontContinuationHealth({
    cmsPagesStatus: "ok",
    cmsPages: [{ id: "page-1" }, { id: "page-2" }],
    categoriesStatus: "degraded",
    categories: [{ id: "cat-1" }],
    productsStatus: "ok",
    products: [{ id: "product-1" }, { id: "product-2" }, { id: "product-3" }],
  });

  assert.deepEqual(summary, {
    cmsStatus: "ok",
    cmsCount: 2,
    categoriesStatus: "degraded",
    categoryCount: 1,
    productsStatus: "ok",
    productCount: 3,
  });
});
