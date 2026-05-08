import assert from "node:assert/strict";
import test from "node:test";
import {
  createStorefrontCartProps,
  createStorefrontCartSummary,
  createStorefrontContinuationSlice,
  createStorefrontContinuationProps,
  createStorefrontContinuationWithCartAndLinkedProps,
  createStorefrontContinuationWithCartProps,
} from "@/features/storefront/route-projections";
import type { PublicStorefrontContext } from "@/features/storefront/public-storefront-context";

function createContext(): PublicStorefrontContext {
  return {
    cmsPagesResult: { data: null, status: "ok" },
    cmsPages: [
      { id: "p1", slug: "one", title: "One", metaTitle: "One", metaDescription: "Desc" },
      { id: "p2", slug: "two", title: "Two", metaTitle: "Two", metaDescription: "Desc" },
    ],
    cmsPagesStatus: "ok",
    categoriesResult: { data: null, status: "ok" },
    categories: [
      { id: "c1", slug: "snacks", name: "Snacks", description: null },
      { id: "c2", slug: "drinks", name: "Drinks", description: null },
    ],
    categoriesStatus: "ok",
    productsResult: { data: null, status: "ok" },
    products: [
      {
        id: "pr1",
        slug: "chips",
        name: "Chips",
        shortDescription: null,
        priceMinor: 100,
        compareAtPriceMinor: 150,
        currency: "EUR",
        primaryImageUrl: "/chips.png",
      },
      {
        id: "pr2",
        slug: "juice",
        name: "Juice",
        shortDescription: null,
        priceMinor: 200,
        compareAtPriceMinor: null,
        currency: "EUR",
        primaryImageUrl: "/juice.png",
      },
    ],
    productsStatus: "degraded",
    storefrontCart: {
      currency: "EUR",
      grandTotalGrossMinor: 1234,
      items: [
        {
          cartItemId: "i1",
          productId: "pr1",
          sku: "chips",
          quantity: 2,
          unitPriceGrossMinor: 617,
          lineTotalGrossMinor: 1234,
          displayName: "Chips",
          href: "/catalog/chips",
          imageUrl: null,
        },
      ],
      lineCount: 1,
      totalNetMinor: 1000,
      totalTaxMinor: 234,
    },
    storefrontCartStatus: "ok",
    cartSnapshots: [],
    cartLinkedProductSlugs: ["chips"],
  };
}

test("createStorefrontCartSummary returns null when no cart exists", () => {
  const context = createContext();
  context.storefrontCart = null;
  context.storefrontCartStatus = "not-found";

  assert.equal(createStorefrontCartSummary(context), null);
});

test("createStorefrontCartSummary returns a canonical lightweight cart projection", () => {
  assert.deepEqual(createStorefrontCartSummary(createContext()), {
    status: "ok",
    itemCount: 1,
    currency: "EUR",
    grandTotalGrossMinor: 1234,
  });
});

test("createStorefrontContinuationSlice keeps statuses and supports count-based slicing", () => {
  const slice = createStorefrontContinuationSlice(createContext(), {
    cmsCount: 1,
    categoryCount: 1,
    productCount: 1,
  });

  assert.equal(slice.cmsPages.length, 1);
  assert.equal(slice.categories.length, 1);
  assert.equal(slice.products.length, 1);
  assert.equal(slice.cmsPagesStatus, "ok");
  assert.equal(slice.categoriesStatus, "ok");
  assert.equal(slice.productsStatus, "degraded");
  assert.deepEqual(slice.cartSummary, {
    status: "ok",
    itemCount: 1,
    currency: "EUR",
    grandTotalGrossMinor: 1234,
  });
});

test("createStorefrontContinuationProps returns canonical continuation fields without cart wiring", () => {
  const props = createStorefrontContinuationProps(createContext(), {
    cmsCount: 1,
    categoryCount: 1,
  });

  assert.deepEqual(props, {
    cmsPages: [
      { id: "p1", slug: "one", title: "One", metaTitle: "One", metaDescription: "Desc" },
    ],
    cmsPagesStatus: "ok",
    categories: [
      { id: "c1", slug: "snacks", name: "Snacks", description: null },
    ],
    categoriesStatus: "ok",
    products: [
      {
        id: "pr1",
        slug: "chips",
        name: "Chips",
        shortDescription: null,
        priceMinor: 100,
        compareAtPriceMinor: 150,
        currency: "EUR",
        primaryImageUrl: "/chips.png",
      },
      {
        id: "pr2",
        slug: "juice",
        name: "Juice",
        shortDescription: null,
        priceMinor: 200,
        compareAtPriceMinor: null,
        currency: "EUR",
        primaryImageUrl: "/juice.png",
      },
    ],
    productsStatus: "degraded",
  });
});

test("createStorefrontContinuationWithCartProps preserves raw cart state for auth and member surfaces", () => {
  const props = createStorefrontContinuationWithCartProps(createContext(), {
    productCount: 1,
  });

  assert.equal(props.products.length, 1);
  assert.equal(props.storefrontCartStatus, "ok");
  assert.equal(props.storefrontCart?.items.length, 1);
});

test("createStorefrontCartProps keeps account surfaces cart-only", () => {
  const props = createStorefrontCartProps(createContext());

  assert.deepEqual(Object.keys(props), ["storefrontCart"]);
  assert.equal(props.storefrontCart?.items.length, 1);
});

test("createStorefrontContinuationWithCartAndLinkedProps also keeps linked product state", () => {
  const props = createStorefrontContinuationWithCartAndLinkedProps(createContext());

  assert.deepEqual(props.cartLinkedProductSlugs, ["chips"]);
  assert.equal(props.storefrontCartStatus, "ok");
});
