import assert from "node:assert/strict";
import test from "node:test";
import { summarizeAccountPageStorefrontFootprint } from "@/features/account/server/get-account-page-context";
import { summarizeCommerceRouteStorefrontFootprint } from "@/features/checkout/server/get-commerce-route-context";

test("summarizeAccountPageStorefrontFootprint keeps public and member storefront state compact", () => {
  assert.equal(
    summarizeAccountPageStorefrontFootprint({
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

test("summarizeCommerceRouteStorefrontFootprint keeps storefront state compact", () => {
  assert.equal(
    summarizeCommerceRouteStorefrontFootprint({
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
