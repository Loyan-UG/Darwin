import assert from "node:assert/strict";
import test from "node:test";
import { summarizeHomeDiscoveryStorefrontFootprint } from "@/features/home/server/get-home-discovery-context";

test("summarizeHomeDiscoveryStorefrontFootprint keeps home discovery state compact", () => {
  assert.equal(
    summarizeHomeDiscoveryStorefrontFootprint({
      storefrontContext: {
        cmsPagesResult: { status: "ok", data: { items: [{ slug: "about" }] } },
        cmsPagesStatus: "ok",
        cmsPages: [{ slug: "about" }],
        categoriesResult: {
          status: "degraded",
          data: { items: [{ slug: "fruit" }] },
        },
        categoriesStatus: "degraded",
        categories: [{ slug: "fruit" }],
        productsResult: {
          status: "ok",
          data: { items: [{ slug: "apples" }, { slug: "pears" }] },
        },
        productsStatus: "ok",
        products: [{ slug: "apples" }, { slug: "pears" }],
        storefrontCart: null,
        storefrontCartStatus: "not-found",
        cartSnapshots: [],
        cartLinkedProductSlugs: [],
      },
    }),
    "cms:ok:1|categories:degraded:1|products:ok:2|cart:not-found",
  );
});
