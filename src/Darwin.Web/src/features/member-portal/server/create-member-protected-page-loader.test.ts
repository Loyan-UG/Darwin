import assert from "node:assert/strict";
import test from "node:test";
import {
  buildMemberProtectedPageLoaderObservationContext,
  buildMemberProtectedPageLoaderSuccessContext,
  createMemberProtectedPageLoaderCore,
} from "@/features/member-portal/server/create-member-protected-page-loader-core";

test("member-protected page loader helper builders keep guest and authorized diagnostics explicit", () => {
  assert.deepEqual(
    buildMemberProtectedPageLoaderObservationContext(
      "/orders/order-1",
      {
        culture: "de-DE",
        id: "order-1",
      },
      {
        hasCanonicalNormalization: true,
      },
    ),
    {
      pageLoaderKind: "member-protected",
      pageLoaderNormalization: "canonical",
      entryRoute: "/orders/order-1",
      culture: "de-DE",
      id: "order-1",
    },
  );

  assert.deepEqual(
    buildMemberProtectedPageLoaderSuccessContext(
      "/orders/order-1",
      {
        entryContext: {
          session: null,
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
        },
        routeContext: null,
      },
      () => ({ detailStatus: "ok" }),
    ),
    {
      pageLoaderKind: "member-protected",
      pageLoaderNormalization: "raw",
      entryRoute: "/orders/order-1",
      authGate: "guest-fallback",
      sessionState: "missing",
      routeContextState: "guest-fallback",
      storefrontFallbackState: "present",
      protectedRouteFootprint: "auth:guest-fallback|route:guest-fallback|storefront:present",
      cmsStatus: "ok",
      cmsCount: 0,
      categoriesStatus: "ok",
      categoryCount: 0,
      productsStatus: "ok",
      productCount: 0,
      heroOfferCount: 0,
      valueOfferCount: 0,
      liveOfferCount: 0,
      baseAssortmentCount: 0,
      promotionLaneFootprint: "hero:0|value:0|live:0|base:0",
      cartStatus: "not-found",
      cartLinkedCount: 0,
    },
  );
});

test("createMemberProtectedPageLoader skips protected route loading when the member session is missing", async () => {
  let protectedExecutions = 0;
  const loader = createMemberProtectedPageLoaderCore({
    operation: "unit-protected-page",
    getContext: (culture: string, id: string) => ({ culture, id }),
    getEntryRoute: (_culture: string, id: string) => `/orders/${id}`,
    loadEntryContext: async () => ({
      session: null,
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
    }),
    summarizeAuthorized: (routeContext) => ({
      detailStatus: routeContext.status,
    }),
    loadRouteContext: async () => {
      protectedExecutions += 1;
      return { status: "ok" };
    },
  });

  const result = await loader("de-DE", "order-1");

  assert.equal(result.entryContext.session, null);
  assert.equal(result.routeContext, null);
  assert.equal(protectedExecutions, 0);
});

test("createMemberProtectedPageLoader keeps authorized route loading behind the shared entry gate", async () => {
  let protectedExecutions = 0;
  const loader = createMemberProtectedPageLoaderCore({
    operation: "unit-protected-page",
    getContext: (culture: string, id: string) => ({ culture, id }),
    getEntryRoute: (_culture: string, id: string) => `/orders/${id}`,
    loadEntryContext: async () => ({
      session: {
        customerId: "customer-1",
        emailAddress: "member@example.com",
        isAuthenticated: true,
      },
      storefrontContext: null,
    }),
    summarizeAuthorized: (routeContext) => ({
      detailStatus: routeContext.status,
    }),
    loadRouteContext: async (_culture: string, id: string) => {
      protectedExecutions += 1;
      return { status: "ok", id };
    },
  });

  const result = await loader("de-DE", "order-1");

  assert.equal(result.entryContext.session?.customerId, "customer-1");
  assert.deepEqual(result.routeContext, { status: "ok", id: "order-1" });
  assert.equal(protectedExecutions, 1);
});

test("createMemberProtectedPageLoader keeps a stable entry route and auth gate across guest and member paths", async () => {
  const guestLoader = createMemberProtectedPageLoaderCore({
    operation: "unit-protected-page",
    getContext: (culture: string, id: string) => ({ culture, id }),
    getEntryRoute: (_culture: string, id: string) => `/orders/${id}`,
    loadEntryContext: async () => ({
      session: null,
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
    }),
    summarizeAuthorized: () => ({ detailStatus: "ok" }),
    loadRouteContext: async () => ({ status: "ok" }),
  });

  const memberLoader = createMemberProtectedPageLoaderCore({
    operation: "unit-protected-page",
    getContext: (culture: string, id: string) => ({ culture, id }),
    getEntryRoute: (_culture: string, id: string) => `/orders/${id}`,
    loadEntryContext: async () => ({
      session: { customerId: "customer-1" },
      storefrontContext: null,
    }),
    summarizeAuthorized: () => ({ detailStatus: "ok" }),
    loadRouteContext: async () => ({ status: "ok" }),
  });

  const guest = await guestLoader("de-DE", "order-1");
  const member = await memberLoader("de-DE", "order-1");

  assert.equal(guest.routeContext, null);
  assert.equal(member.routeContext?.status, "ok");
});

test("createMemberProtectedPageLoader normalizes equivalent arguments before caching", async () => {
  const loader = createMemberProtectedPageLoaderCore({
    operation: "unit-protected-page",
    normalizeArgs: (culture: string, id: string) => [culture.trim(), id.trim()] as [string, string],
    getContext: (culture: string, id: string) => ({ culture, id }),
    getEntryRoute: (_culture: string, id: string) => `/orders/${id}`,
    loadEntryContext: async () => ({
      session: { customerId: "customer-1" },
      storefrontContext: null,
    }),
    summarizeAuthorized: (routeContext) => ({ detailStatus: routeContext.status }),
    loadRouteContext: async (_culture: string, id: string) => ({ status: "ok", id }),
  });

  const [first, second] = await Promise.all([
    loader("de-DE", "order-1"),
    loader(" de-DE ", " order-1 "),
  ]);

  assert.equal(first.routeContext?.id, "order-1");
  assert.equal(second.routeContext?.id, "order-1");
});

test("createMemberProtectedPageLoader feeds normalized args into entry route and diagnostics", async () => {
  let contextSnapshot: Record<string, unknown> | undefined;
  let successSnapshot: Record<string, unknown> | undefined;
  let entryRouteSnapshot: string | undefined;

  const loader = createMemberProtectedPageLoaderCore({
    operation: "unit-protected-page",
    normalizeArgs: (culture: string, id: string) =>
      [culture.trim(), id.trim()] as [string, string],
    getContext: (culture: string, id: string) => {
      contextSnapshot = { culture, id };
      return { culture, id };
    },
    getEntryRoute: (_culture: string, id: string) => {
      entryRouteSnapshot = `/orders/${id}`;
      return entryRouteSnapshot;
    },
    loadEntryContext: async () => ({
      session: { customerId: "customer-1" },
      storefrontContext: null,
    }),
    summarizeAuthorized: (routeContext) => {
      successSnapshot = {
        detailStatus: routeContext.status,
        id: routeContext.id,
      };

      return {
        detailStatus: routeContext.status,
        detailId: routeContext.id,
      };
    },
    loadRouteContext: async (_culture: string, id: string) => ({
      status: "ok",
      id,
    }),
  });

  const result = await loader(" de-DE ", " order-1 ");

  assert.equal(result.routeContext?.id, "order-1");
  assert.deepEqual(contextSnapshot, {
    culture: "de-DE",
    id: "order-1",
  });
  assert.equal(entryRouteSnapshot, "/orders/order-1");
  assert.deepEqual(successSnapshot, {
    detailStatus: "ok",
    id: "order-1",
  });
});

test("createMemberProtectedPageLoader emits raw protected route diagnostics for guest fallbacks", async () => {
  const warnings: Array<{ message: string; detail: Record<string, unknown> }> = [];
  const originalWarn = console.warn;
  console.warn = ((message, detail) => {
    warnings.push({ message, detail });
  }) as typeof console.warn;

  try {
    const loader = createMemberProtectedPageLoaderCore({
      operation: "unit-protected-page",
      thresholdMs: 0,
      getContext: (culture: string, id: string) => ({ culture, id }),
      getEntryRoute: (_culture: string, id: string) => `/orders/${id}`,
      loadEntryContext: async () => ({
        session: null,
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
      }),
      summarizeAuthorized: () => ({
        detailStatus: "ok",
      }),
      loadRouteContext: async () => ({
        status: "ok",
      }),
    });

    const result = await loader("de-DE", "order-1");

    assert.equal(result.routeContext, null);
    assert.equal(warnings.length, 1);
    assert.equal(warnings[0]?.message, "Darwin.Web slow operation");
    assert.deepEqual(warnings[0]?.detail, {
      area: "member-protected-page-context",
      operation: "unit-protected-page",
      operationKey: "member-protected-page-context:unit-protected-page",
      durationMs: warnings[0]?.detail.durationMs,
      durationBand: "very-slow",
      healthState: "degraded",
      outcomeKind: "slow-degraded-success",
      signalKind: "performance-and-health",
      attentionLevel: "high",
      suggestedAction: "inspect-slow-and-degraded-dependencies",
      degradedStatusCount: 1,
      degradedStatuses: { cartStatus: "not-found" },
      degradedStatusKeys: ["cartStatus"],
      degradedSurfaceCount: 1,
      degradedSurfaceKeys: ["cart"],
      degradedSurfaceFootprint: "cart:not-found",
      primaryDegradedStatusKey: "cartStatus",
      primaryDegradedSurface: "cart",
      pageLoaderKind: "member-protected",
      pageLoaderNormalization: "raw",
      entryRoute: "/orders/order-1",
      culture: "de-DE",
      id: "order-1",
      authGate: "guest-fallback",
      sessionState: "missing",
      routeContextState: "guest-fallback",
      storefrontFallbackState: "present",
      protectedRouteFootprint: "auth:guest-fallback|route:guest-fallback|storefront:present",
      cmsStatus: "ok",
      cmsCount: 0,
      categoriesStatus: "ok",
      categoryCount: 0,
      productsStatus: "ok",
      productCount: 0,
      heroOfferCount: 0,
      valueOfferCount: 0,
      liveOfferCount: 0,
      baseAssortmentCount: 0,
      promotionLaneFootprint: "hero:0|value:0|live:0|base:0",
      cartStatus: "not-found",
      cartLinkedCount: 0,
    });
  } finally {
    console.warn = originalWarn;
  }
});

test("createMemberProtectedPageLoader emits protected route diagnostics for guest fallbacks", async () => {
  const warnings: Array<{ message: string; detail: Record<string, unknown> }> = [];
  const originalWarn = console.warn;
  console.warn = ((message, detail) => {
    warnings.push({ message, detail });
  }) as typeof console.warn;

  try {
    const loader = createMemberProtectedPageLoaderCore({
      operation: "unit-protected-page",
      thresholdMs: 0,
      normalizeArgs: (culture: string, id: string) =>
        [culture.trim(), id.trim()] as [string, string],
      getContext: (culture: string, id: string) => ({ culture, id }),
      getEntryRoute: (_culture: string, id: string) => `/orders/${id}`,
      loadEntryContext: async () => ({
        session: null,
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
      }),
      summarizeAuthorized: () => ({
        detailStatus: "ok",
      }),
      loadRouteContext: async () => ({
        status: "ok",
      }),
    });

    const result = await loader(" de-DE ", " order-1 ");

    assert.equal(result.routeContext, null);
    assert.equal(warnings.length, 1);
    assert.equal(warnings[0]?.message, "Darwin.Web slow operation");
    assert.deepEqual(warnings[0]?.detail, {
      area: "member-protected-page-context",
      operation: "unit-protected-page",
      operationKey: "member-protected-page-context:unit-protected-page",
      durationMs: warnings[0]?.detail.durationMs,
      durationBand: "very-slow",
      healthState: "degraded",
      outcomeKind: "slow-degraded-success",
      signalKind: "performance-and-health",
      attentionLevel: "high",
      suggestedAction: "inspect-slow-and-degraded-dependencies",
      degradedStatusCount: 1,
      degradedStatuses: { cartStatus: "not-found" },
      degradedStatusKeys: ["cartStatus"],
      degradedSurfaceCount: 1,
      degradedSurfaceKeys: ["cart"],
      degradedSurfaceFootprint: "cart:not-found",
      primaryDegradedStatusKey: "cartStatus",
      primaryDegradedSurface: "cart",
      pageLoaderKind: "member-protected",
      pageLoaderNormalization: "canonical",
      entryRoute: "/orders/order-1",
      culture: "de-DE",
      id: "order-1",
      authGate: "guest-fallback",
      sessionState: "missing",
      routeContextState: "guest-fallback",
      storefrontFallbackState: "present",
      protectedRouteFootprint: "auth:guest-fallback|route:guest-fallback|storefront:present",
      cmsStatus: "ok",
      cmsCount: 0,
      categoriesStatus: "ok",
      categoryCount: 0,
      productsStatus: "ok",
      productCount: 0,
      heroOfferCount: 0,
      valueOfferCount: 0,
      liveOfferCount: 0,
      baseAssortmentCount: 0,
      promotionLaneFootprint: "hero:0|value:0|live:0|base:0",
      cartStatus: "not-found",
      cartLinkedCount: 0,
    });
  } finally {
    console.warn = originalWarn;
  }
});

test("createMemberProtectedPageLoader emits raw protected route diagnostics for authorized members", async () => {
  const warnings: Array<{ message: string; detail: Record<string, unknown> }> = [];
  const originalWarn = console.warn;
  console.warn = ((message, detail) => {
    warnings.push({ message, detail });
  }) as typeof console.warn;

  try {
    const loader = createMemberProtectedPageLoaderCore({
      operation: "unit-protected-page",
      thresholdMs: 0,
      getContext: (culture: string, id: string) => ({ culture, id }),
      getEntryRoute: (_culture: string, id: string) => `/orders/${id}`,
      loadEntryContext: async () => ({
        session: { customerId: "customer-1" },
        storefrontContext: null,
      }),
      summarizeAuthorized: (routeContext) => ({
        detailStatus: routeContext.status,
        detailId: routeContext.id,
      }),
      loadRouteContext: async (_culture: string, id: string) => ({
        status: "ok",
        id,
      }),
    });

    const result = await loader("de-DE", "order-1");

    assert.equal(result.routeContext?.id, "order-1");
    assert.equal(warnings.length, 1);
    assert.equal(warnings[0]?.message, "Darwin.Web slow operation");
    assert.deepEqual(warnings[0]?.detail, {
      area: "member-protected-page-context",
      operation: "unit-protected-page",
      operationKey: "member-protected-page-context:unit-protected-page",
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
      pageLoaderKind: "member-protected",
      pageLoaderNormalization: "raw",
      entryRoute: "/orders/order-1",
      culture: "de-DE",
      id: "order-1",
      authGate: "authorized",
      sessionState: "present",
      routeContextState: "loaded",
      storefrontFallbackState: "missing",
      protectedRouteFootprint: "auth:authorized|route:loaded|storefront:missing",
      detailStatus: "ok",
      detailId: "order-1",
    });
  } finally {
    console.warn = originalWarn;
  }
});

test("createMemberProtectedPageLoader emits protected route diagnostics for authorized members", async () => {
  const warnings: Array<{ message: string; detail: Record<string, unknown> }> = [];
  const originalWarn = console.warn;
  console.warn = ((message, detail) => {
    warnings.push({ message, detail });
  }) as typeof console.warn;

  try {
    const loader = createMemberProtectedPageLoaderCore({
      operation: "unit-protected-page",
      thresholdMs: 0,
      normalizeArgs: (culture: string, id: string) =>
        [culture.trim(), id.trim()] as [string, string],
      getContext: (culture: string, id: string) => ({ culture, id }),
      getEntryRoute: (_culture: string, id: string) => `/orders/${id}`,
      loadEntryContext: async () => ({
        session: { customerId: "customer-1" },
        storefrontContext: null,
      }),
      summarizeAuthorized: (routeContext) => ({
        detailStatus: routeContext.status,
        detailId: routeContext.id,
      }),
      loadRouteContext: async (_culture: string, id: string) => ({
        status: "ok",
        id,
      }),
    });

    const result = await loader(" de-DE ", " order-1 ");

    assert.equal(result.routeContext?.id, "order-1");
    assert.equal(warnings.length, 1);
    assert.equal(warnings[0]?.message, "Darwin.Web slow operation");
    assert.deepEqual(warnings[0]?.detail, {
      area: "member-protected-page-context",
      operation: "unit-protected-page",
      operationKey: "member-protected-page-context:unit-protected-page",
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
      pageLoaderKind: "member-protected",
      pageLoaderNormalization: "canonical",
      entryRoute: "/orders/order-1",
      culture: "de-DE",
      id: "order-1",
      authGate: "authorized",
      sessionState: "present",
      routeContextState: "loaded",
      storefrontFallbackState: "missing",
      protectedRouteFootprint: "auth:authorized|route:loaded|storefront:missing",
      detailStatus: "ok",
      detailId: "order-1",
    });
  } finally {
    console.warn = originalWarn;
  }
});




