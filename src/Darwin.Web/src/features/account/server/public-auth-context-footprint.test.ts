import assert from "node:assert/strict";
import test from "node:test";
import { summarizePublicAuthStorefrontFootprint } from "@/features/account/server/get-public-auth-route-context";

test("summarizePublicAuthStorefrontFootprint keeps public auth storefront state compact", () => {
  assert.equal(
    summarizePublicAuthStorefrontFootprint({
      storefrontContext: {
        cmsPagesResult: { status: "ok", data: { items: [{ slug: "about" }] } },
        cmsPagesStatus: "ok",
        cmsPages: [{ slug: "about" }],
        categoriesResult: { status: "ok", data: { items: [{ slug: "fruit" }] } },
        categoriesStatus: "ok",
        categories: [{ slug: "fruit" }],
        productsResult: { status: "degraded", data: { items: [{ slug: "apples" }, { slug: "pears" }] } },
        productsStatus: "degraded",
        products: [{ slug: "apples" }, { slug: "pears" }],
        storefrontCart: null,
        storefrontCartStatus: "not-found",
        cartSnapshots: [],
        cartLinkedProductSlugs: [],
      },
    }),
    "cms:ok:1|categories:ok:1|products:degraded:2|cart:not-found",
  );
});
