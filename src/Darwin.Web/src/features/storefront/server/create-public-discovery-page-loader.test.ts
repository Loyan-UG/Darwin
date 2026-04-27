import assert from "node:assert/strict";
import test from "node:test";
import {
  buildPublicDiscoveryPageLoaderObservationContext,
  buildPublicDiscoveryPageLoaderSuccessContext,
  createPublicDiscoveryPageLoaderCore,
} from "@/features/storefront/server/create-public-discovery-page-loader-core";
import { buildContinuationSliceFootprint } from "@/lib/page-loader-diagnostics";

test("public-discovery page loader helper builders keep canonical observation and continuation diagnostics explicit", () => {
  assert.deepEqual(
    buildPublicDiscoveryPageLoaderObservationContext(
      {
        culture: "de-DE",
        slug: "faq",
      },
      {
        hasCanonicalNormalization: true,
      },
    ),
    {
      pageLoaderKind: "public-discovery",
      pageLoaderNormalization: "canonical",
      culture: "de-DE",
      slug: "faq",
    },
  );

  assert.deepEqual(
    buildPublicDiscoveryPageLoaderSuccessContext(
      {
        cmsPages: [{ slug: "faq" }],
        categories: [{ slug: "bakery" }],
        products: [{ slug: "bread" }],
        cartSummary: null,
      } as never,
      {
        slug: "faq",
      },
      {
        hasCanonicalNormalization: true,
      },
    ),
    {
      pageLoaderKind: "public-discovery",
      pageLoaderNormalization: "canonical",
      continuationCmsCount: 1,
      continuationCategoryCount: 1,
      continuationProductCount: 1,
      continuationCartState: "missing",
      continuationSurfaceFootprint: "cms:1|categories:1|products:1|cart:missing",
      slug: "faq",
    },
  );
});

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
        continuationSurfaceFootprint: buildContinuationSliceFootprint({
          cmsCount: result.continuationSlice.cmsPages.length,
          categoryCount: result.continuationSlice.categories.length,
          productCount: result.continuationSlice.products.length,
          cartState: result.continuationSlice.cartSummary ? "present" : "missing",
        }),
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
    continuationSurfaceFootprint: "cms:1|categories:1|products:1|cart:missing",
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

test("createPublicDiscoveryPageLoader emits canonical loader diagnostics for slow success paths", async () => {
  const warnings: Array<{ message: string; detail: Record<string, unknown> }> = [];
  const originalWarn = console.warn;
  console.warn = ((message, detail) => {
    warnings.push({ message, detail });
  }) as typeof console.warn;

  try {
    const loader = createPublicDiscoveryPageLoaderCore({
      area: "unit-discovery-page",
      operation: "observe-page",
      thresholdMs: 0,
      normalizeArgs: (culture: string, slug: string) =>
        [culture.trim(), slug.trim()] as [string, string],
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
    });

    const result = await loader(" de-DE ", " faq ");

    assert.equal(result.slug, "faq");
    assert.equal(warnings.length, 1);
    assert.equal(warnings[0]?.message, "Darwin.Web slow operation");
    assert.deepEqual(warnings[0]?.detail, {
      area: "unit-discovery-page",
      operation: "observe-page",
      operationKey: "unit-discovery-page:observe-page",
      durationMs: warnings[0]?.detail.durationMs,
      durationBand: "very-slow",
      healthState: "healthy",
      outcomeKind: "slow-success",
      signalKind: "performance",
      attentionLevel: "high",
      suggestedAction: "inspect-slow-path",
      degradedStatusCount: 0,
      degradedStatuses: undefined,
      degradedStatusKeys: undefined,
      degradedSurfaceCount: 0,
      degradedSurfaceKeys: undefined,
      degradedSurfaceFootprint: undefined,
      primaryDegradedStatusKey: undefined,
      primaryDegradedSurface: undefined,
      pageLoaderKind: "public-discovery",
      pageLoaderNormalization: "canonical",
      culture: "de-DE",
      slug: "faq",
      continuationCmsCount: 1,
      continuationCategoryCount: 1,
      continuationProductCount: 1,
      continuationCartState: "missing",
      continuationSurfaceFootprint: "cms:1|categories:1|products:1|cart:missing",
    });
  } finally {
    console.warn = originalWarn;
  }
});

test("createPublicDiscoveryPageLoader emits raw loader diagnostics for slow success paths", async () => {
  const warnings: Array<{ message: string; detail: Record<string, unknown> }> = [];
  const originalWarn = console.warn;
  console.warn = ((message, detail) => {
    warnings.push({ message, detail });
  }) as typeof console.warn;

  try {
    const loader = createPublicDiscoveryPageLoaderCore({
      area: "unit-discovery-page",
      operation: "observe-page",
      thresholdMs: 0,
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
    });

    const result = await loader("de-DE", "faq");

    assert.equal(result.slug, "faq");
    assert.equal(warnings.length, 1);
    assert.equal(warnings[0]?.message, "Darwin.Web slow operation");
    assert.deepEqual(warnings[0]?.detail, {
      area: "unit-discovery-page",
      operation: "observe-page",
      operationKey: "unit-discovery-page:observe-page",
      durationMs: warnings[0]?.detail.durationMs,
      durationBand: "very-slow",
      healthState: "healthy",
      outcomeKind: "slow-success",
      signalKind: "performance",
      attentionLevel: "high",
      suggestedAction: "inspect-slow-path",
      degradedStatusCount: 0,
      degradedStatuses: undefined,
      degradedStatusKeys: undefined,
      degradedSurfaceCount: 0,
      degradedSurfaceKeys: undefined,
      degradedSurfaceFootprint: undefined,
      primaryDegradedStatusKey: undefined,
      primaryDegradedSurface: undefined,
      pageLoaderKind: "public-discovery",
      pageLoaderNormalization: "raw",
      culture: "de-DE",
      slug: "faq",
      continuationCmsCount: 1,
      continuationCategoryCount: 1,
      continuationProductCount: 1,
      continuationCartState: "missing",
      continuationSurfaceFootprint: "cms:1|categories:1|products:1|cart:missing",
    });
  } finally {
    console.warn = originalWarn;
  }
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



