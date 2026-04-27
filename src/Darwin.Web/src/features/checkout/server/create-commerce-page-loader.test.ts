import assert from "node:assert/strict";
import test from "node:test";
import {
  buildCommercePageLoaderObservationContext,
  buildCommercePageLoaderSuccessContext,
  createCommercePageLoaderCore,
} from "@/features/checkout/server/create-commerce-page-loader-core";

test("commerce page loader helper builders keep canonical and raw diagnostics explicit", () => {
  assert.deepEqual(
    buildCommercePageLoaderObservationContext(
      {
        culture: "de-DE",
        page: "checkout",
      },
      {
        hasCanonicalNormalization: true,
      },
    ),
    {
      pageLoaderKind: "commerce",
      pageLoaderNormalization: "canonical",
      culture: "de-DE",
      page: "checkout",
    },
  );

  assert.deepEqual(
    buildCommercePageLoaderSuccessContext({
      step: "ready:checkout",
      cartState: "present",
    }),
    {
      pageLoaderKind: "commerce",
      pageLoaderNormalization: "raw",
      step: "ready:checkout",
      cartState: "present",
    },
  );
});

test("createCommercePageLoader keeps argument-specific commerce results distinct", async () => {
  let executions = 0;
  const loader = createCommercePageLoaderCore({
    operation: "unit-commerce-page",
    getContext: (culture: string, page: string) => ({ culture, page }),
    getSuccessContext: (result) => ({
      step: result.step,
    }),
    load: async (culture: string, page: string) => {
      executions += 1;
      return {
        step: `${culture}:${page}:${executions}`,
      };
    },
  });

  const first = await loader("de-DE", "cart");
  const second = await loader("de-DE", "cart");
  const third = await loader("en-US", "checkout");

  assert.match(first.step, /^de-DE:cart:/);
  assert.match(second.step, /^de-DE:cart:/);
  assert.match(third.step, /^en-US:checkout:/);
  assert.equal(first.step !== third.step, true);
  assert.equal(executions >= 2, true);
});

test("createCommercePageLoader keeps commerce loader results cache-scoped without mutating the result shape", async () => {
  const loader = createCommercePageLoaderCore({
    operation: "unit-commerce-page",
    getContext: (culture: string, page: string) => ({ culture, page }),
    getSuccessContext: (result) => ({
      step: result.step,
    }),
    load: async (_culture: string, page: string) => ({
      step: page,
      route: `/test/${page}`,
    }),
  });

  const result = await loader("de-DE", "cart");

  assert.deepEqual(result, {
    step: "cart",
    route: "/test/cart",
  });
});

test("createCommercePageLoader normalizes equivalent arguments before caching", async () => {
  const loader = createCommercePageLoaderCore({
    operation: "unit-commerce-page",
    normalizeArgs: (culture: string, page: string) => [culture.trim(), page.trim()] as [string, string],
    getContext: (culture: string, page: string) => ({ culture, page }),
    getSuccessContext: () => ({}),
    load: async (_culture: string, page: string) => ({
      step: page,
    }),
  });

  const [first, second] = await Promise.all([
    loader("de-DE", "cart"),
    loader(" de-DE ", " cart "),
  ]);

  assert.equal(first.step, "cart");
  assert.equal(second.step, "cart");
});

test("createCommercePageLoader emits canonical loader diagnostics for slow success paths", async () => {
  const warnings: Array<{ message: string; detail: Record<string, unknown> }> = [];
  const originalWarn = console.warn;
  console.warn = ((message, detail) => {
    warnings.push({ message, detail });
  }) as typeof console.warn;

  try {
    const loader = createCommercePageLoaderCore({
      operation: "unit-commerce-page",
      thresholdMs: 0,
      normalizeArgs: (culture: string, page: string) =>
        [culture.trim(), page.trim()] as [string, string],
      getContext: (culture: string, page: string) => ({ culture, page }),
      getSuccessContext: (result) => ({
        step: result.step,
      }),
      load: async (_culture: string, page: string) => ({
        step: `ready:${page}`,
        page,
      }),
    });

    const result = await loader(" de-DE ", " checkout ");

    assert.deepEqual(result, {
      step: "ready:checkout",
      page: "checkout",
    });
    assert.equal(warnings.length, 1);
    assert.equal(warnings[0]?.message, "Darwin.Web slow operation");
    assert.deepEqual(warnings[0]?.detail, {
      area: "commerce-page-context",
      operation: "unit-commerce-page",
      operationKey: "commerce-page-context:unit-commerce-page",
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
      pageLoaderKind: "commerce",
      pageLoaderNormalization: "canonical",
      culture: "de-DE",
      page: "checkout",
      step: "ready:checkout",
    });
  } finally {
    console.warn = originalWarn;
  }
});

test("createCommercePageLoader emits raw loader diagnostics for slow success paths", async () => {
  const warnings: Array<{ message: string; detail: Record<string, unknown> }> = [];
  const originalWarn = console.warn;
  console.warn = ((message, detail) => {
    warnings.push({ message, detail });
  }) as typeof console.warn;

  try {
    const loader = createCommercePageLoaderCore({
      operation: "unit-commerce-page",
      thresholdMs: 0,
      getContext: (culture: string, page: string) => ({ culture, page }),
      getSuccessContext: (result) => ({
        step: result.step,
      }),
      load: async (_culture: string, page: string) => ({
        step: `ready:${page}`,
        page,
      }),
    });

    const result = await loader("de-DE", "cart");

    assert.deepEqual(result, {
      step: "ready:cart",
      page: "cart",
    });
    assert.equal(warnings.length, 1);
    assert.equal(warnings[0]?.message, "Darwin.Web slow operation");
    assert.deepEqual(warnings[0]?.detail, {
      area: "commerce-page-context",
      operation: "unit-commerce-page",
      operationKey: "commerce-page-context:unit-commerce-page",
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
      pageLoaderKind: "commerce",
      pageLoaderNormalization: "raw",
      culture: "de-DE",
      page: "cart",
      step: "ready:cart",
    });
  } finally {
    console.warn = originalWarn;
  }
});

test("createCommercePageLoader feeds normalized args into context and success diagnostics", async () => {
  let contextSnapshot: Record<string, unknown> | undefined;
  let successSnapshot: Record<string, unknown> | undefined;

  const loader = createCommercePageLoaderCore({
    operation: "unit-commerce-page",
    normalizeArgs: (culture: string, page: string) =>
      [culture.trim(), page.trim()] as [string, string],
    getContext: (culture: string, page: string) => {
      contextSnapshot = { culture, page };
      return { culture, page };
    },
    getSuccessContext: (result) => {
      successSnapshot = { step: result.step, page: result.page };
      return { step: result.step };
    },
    load: async (_culture: string, page: string) => ({
      step: `ready:${page}`,
      page,
    }),
  });

  const result = await loader(" de-DE ", " checkout ");

  assert.deepEqual(result, {
    step: "ready:checkout",
    page: "checkout",
  });
  assert.deepEqual(contextSnapshot, {
    culture: "de-DE",
    page: "checkout",
  });
  assert.deepEqual(successSnapshot, {
    step: "ready:checkout",
    page: "checkout",
  });
});


