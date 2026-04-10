import assert from "node:assert/strict";
import test from "node:test";
import { summarizeAccountPageStorefrontSupport } from "@/features/account/server/get-account-page-context";
import { summarizeCommerceRouteStorefrontSupport } from "@/features/checkout/server/get-commerce-route-context";

test("summarizeAccountPageStorefrontSupport keeps public and member storefront support readable", () => {
  assert.equal(
    summarizeAccountPageStorefrontSupport({
      session: null,
      publicRouteContext: {
        storefrontContext: {
          cmsPagesResult: { status: "ok", data: { items: [{ slug: "about" }] } },
          cmsPagesStatus: "ok",
          cmsPages: [{ slug: "about" }],
          categoriesResult: { status: "ok", data: { items: [{ slug: "fruit" }] } },
          categoriesStatus: "ok",
          categories: [{ slug: "fruit" }],
          productsResult: { status: "degraded", data: { items: [{ slug: "apples" }] } },
          productsStatus: "degraded",
          products: [{ slug: "apples" }],
          storefrontCart: null,
          storefrontCartStatus: "not-found",
          cartSnapshots: [],
          cartLinkedProductSlugs: [],
        },
      },
      memberRouteContext: null,
    }),
    "session:missing|cms:ok:1|categories:ok:1|products:degraded:1|cart:not-found",
  );
});

test("summarizeCommerceRouteStorefrontSupport keeps storefront continuity readable", () => {
  assert.equal(
    summarizeCommerceRouteStorefrontSupport({
      storefrontContext: {
        cmsPagesResult: { status: "ok", data: { items: [{ slug: "about" }] } },
        cmsPagesStatus: "ok",
        cmsPages: [{ slug: "about" }],
        categoriesResult: { status: "degraded", data: { items: [{ slug: "fruit" }] } },
        categoriesStatus: "degraded",
        categories: [{ slug: "fruit" }],
        productsResult: { status: "ok", data: { items: [{ slug: "apples" }, { slug: "pears" }] } },
        productsStatus: "ok",
        products: [{ slug: "apples" }, { slug: "pears" }],
        storefrontCart: null,
        storefrontCartStatus: "ok",
        cartSnapshots: [],
        cartLinkedProductSlugs: [],
      },
    }),
    "cms:ok:1|categories:degraded:1|products:ok:2|cart:ok",
  );
});
