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

test("createPublicDiscoveryPageLoader adds canonical loader diagnostics around the custom success summary", async () => {
  let successSummary: Record<string, unknown> | undefined;
  const loader = createPublicDiscoveryPageLoaderCore({
    area: "unit-discovery-page",
    operation: "load-page",
    getContext: (culture: string, slug: string) => ({ culture, slug }),
    getSuccessContext: (result) => ({
      slug: result.slug,
    }),
    loadRouteContext: async (_culture: string, slug: string) => ({
      slug,
      storefrontContext: {
        cmsPagesResult: { status: "ok", data: { items: [{ slug: "faq" }] } },
        cmsPagesStatus: "ok",
        cmsPages: [{ slug: "faq" }],
        categoriesResult: { status: "ok", data: { items: [{ slug: "bakery" }] } },
        categoriesStatus: "ok",
        categories: [{ slug: "bakery" }],
        productsResult: { status: "ok", data: { items: [{ slug: "bread" }] } },
        productsStatus: "ok",
        products: [{ slug: "bread" }],
        storefrontCart: null,
        storefrontCartStatus: "not-found",
        cartSnapshots: [],
        cartLinkedProductSlugs: [],
      },
    }),
    thresholdMs: 999999,
  });

  await loader("de-DE", "faq");
  const observed = createPublicDiscoveryPageLoaderCore({
    area: "unit-discovery-page",
    operation: "observe-page",
    getContext: () => ({}),
    getSuccessContext: (result) => {
      successSummary = {
        pageLoaderKind: "public-discovery",
        continuationCmsCount: result.continuationSlice.cmsPages.length,
        continuationCategoryCount: result.continuationSlice.categories.length,
        continuationProductCount: result.continuationSlice.products.length,
        continuationCartState: result.continuationSlice.cartSummary ? "present" : "missing",
      };

      return {};
    },
    loadRouteContext: async () => ({
      storefrontContext: {
        cmsPagesResult: { status: "ok", data: { items: [{ slug: "faq" }] } },
        cmsPagesStatus: "ok",
        cmsPages: [{ slug: "faq" }],
        categoriesResult: { status: "ok", data: { items: [{ slug: "bakery" }] } },
        categoriesStatus: "ok",
        categories: [{ slug: "bakery" }],
        productsResult: { status: "ok", data: { items: [{ slug: "bread" }] } },
        productsStatus: "ok",
        products: [{ slug: "bread" }],
        storefrontCart: null,
        storefrontCartStatus: "not-found",
        cartSnapshots: [],
        cartLinkedProductSlugs: [],
      },
    }),
  });

  await observed("de-DE", "faq");

  assert.deepEqual(successSummary, {
    pageLoaderKind: "public-discovery",
    continuationCmsCount: 1,
    continuationCategoryCount: 1,
    continuationProductCount: 1,
    continuationCartState: "missing",
  });
});

test("createPublicDiscoveryPageLoader normalizes equivalent arguments before caching", async () => {
  const loader = createPublicDiscoveryPageLoaderCore({
    area: "unit-discovery-page",
    operation: "load-page",
    normalizeArgs: (culture: string, slug: string) => [culture.trim(), slug.trim()] as [string, string],
    getContext: (culture: string, slug: string) => ({ culture, slug }),
    getSuccessContext: () => ({}),
    loadRouteContext: async (_culture: string, slug: string) => {
      return {
        slug,
        storefrontContext: {
          cmsPagesResult: { status: "ok", data: { items: [] } },
          cmsPagesStatus: "ok",
          cmsPages: [],
          categoriesResult: { status: "ok", data: { items: [] } },
          categoriesStatus: "ok",
          categories: [],
          productsResult: { status: "ok", data: { items: [] } },
          productsStatus: "ok",
          products: [],
          storefrontCart: null,
          storefrontCartStatus: "not-found",
          cartSnapshots: [],
          cartLinkedProductSlugs: [],
        },
      };
    },
  });

  const [first, second] = await Promise.all([
    loader("de-DE", "faq"),
    loader(" de-DE ", " faq "),
  ]);

  assert.equal(first.slug, "faq");
  assert.equal(second.slug, "faq");
});

test("createPublicDiscoveryPageLoader feeds normalized args into context and success diagnostics", async () => {
  let contextSnapshot: Record<string, unknown> | undefined;
  let successSnapshot: Record<string, unknown> | undefined;

  const loader = createPublicDiscoveryPageLoaderCore({
    area: "unit-discovery-page",
    operation: "load-page",
    normalizeArgs: (culture: string, slug: string) =>
      [culture.trim(), slug.trim()] as [string, string],
    getContext: (culture: string, slug: string) => {
      contextSnapshot = { culture, slug };
      return { culture, slug };
    },
    getSuccessContext: (result) => {
      successSnapshot = {
        slug: result.slug,
        continuationCmsCount: result.continuationSlice.cmsPages.length,
      };

      return {
        slug: result.slug,
      };
    },
    loadRouteContext: async (_culture: string, slug: string) => ({
      slug,
      storefrontContext: {
        cmsPagesResult: { status: "ok", data: { items: [{ slug: "faq" }] } },
        cmsPagesStatus: "ok",
        cmsPages: [{ slug: "faq" }],
        categoriesResult: { status: "ok", data: { items: [] } },
        categoriesStatus: "ok",
        categories: [],
        productsResult: { status: "ok", data: { items: [] } },
        productsStatus: "ok",
        products: [],
        storefrontCart: null,
        storefrontCartStatus: "not-found",
        cartSnapshots: [],
        cartLinkedProductSlugs: [],
      },
    }),
  });

  const result = await loader(" de-DE ", " faq ");

  assert.equal(result.slug, "faq");
  assert.deepEqual(contextSnapshot, {
    culture: "de-DE",
    slug: "faq",
  });
  assert.deepEqual(successSnapshot, {
    slug: "faq",
    continuationCmsCount: 1,
  });
});
