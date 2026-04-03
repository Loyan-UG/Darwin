import assert from "node:assert/strict";
import test from "node:test";
import { createPublicDiscoveryPageLoaderCore } from "@/features/storefront/server/create-public-discovery-page-loader-core";

test("createPublicDiscoveryPageLoader keeps route context and adds the configured continuation slice", async () => {
  let executions = 0;
  const loader = createPublicDiscoveryPageLoaderCore({
    area: "unit-discovery-page",
    operation: "load-page",
    getContext: (culture: string, slug: string) => ({ culture, slug }),
    getSuccessContext: (result) => ({
      cmsCount: result.continuationSlice.cmsPages.length,
      productCount: result.continuationSlice.products.length,
    }),
    sliceOptions: {
      cmsCount: 1,
      productCount: 2,
    },
    loadRouteContext: async (_culture: string, slug: string) => {
      executions += 1;

      return {
        slug,
        storefrontContext: {
          cmsPagesResult: { status: "ok", data: { items: [{ slug: "faq" }, { slug: "about" }] } },
          cmsPagesStatus: "ok",
          cmsPages: [{ slug: "faq" }, { slug: "about" }],
          categoriesResult: { status: "ok", data: { items: [{ slug: "bakery" }] } },
          categoriesStatus: "ok",
          categories: [{ slug: "bakery" }],
          productsResult: {
            status: "ok",
            data: { items: [{ slug: "baguette" }, { slug: "croissant" }, { slug: "cake" }] },
          },
          productsStatus: "ok",
          products: [{ slug: "baguette" }, { slug: "croissant" }, { slug: "cake" }],
          storefrontCart: null,
          storefrontCartStatus: "not-found",
          cartSnapshots: [],
          cartLinkedProductSlugs: [],
        },
      };
    },
  });

  const result = await loader("de-DE", "faq");

  assert.equal(result.slug, "faq");
  assert.equal(result.continuationSlice.cmsPages.length, 1);
  assert.equal(result.continuationSlice.products.length, 2);
  assert.equal(executions, 1);
});
