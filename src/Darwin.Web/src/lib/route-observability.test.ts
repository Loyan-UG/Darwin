import test from "node:test";
import assert from "node:assert/strict";
import {
  buildObservedOutcomeDetail,
  buildObservedOperationKey,
  getDegradedStatusEntries,
  getAttentionLevel,
  getDegradedSurfaceFootprint,
  getDurationBand,
  getSignalKind,
  getSuggestedAction,
  observeAsyncOperation,
  shouldLogDegradedOperations,
} from "@/lib/route-observability";

test("route observability helpers classify duration, signal, attention, and suggested action directly", () => {
  assert.equal(buildObservedOperationKey("home", "compose"), "home:compose");

  assert.equal(
    getDegradedSurfaceFootprint([
      ["menuStatus", "fallback"],
      ["productsStatus", "stale"],
    ]),
    "menu:fallback|products:stale",
  );
  assert.equal(
    getDegradedSurfaceFootprint([
      ["shell", "fallback"],
      ["cartStatus", "missing"],
    ]),
    "shell:fallback|cart:missing",
  );
  assert.equal(getDegradedSurfaceFootprint([]), undefined);

  assert.equal(getDurationBand(10, 50), "within-threshold");
  assert.equal(getDurationBand(60, 50), "slow");
  assert.equal(getDurationBand(180, 50), "very-slow");

  assert.equal(getSignalKind({}), "normal");
  assert.equal(getSignalKind({ isSlow: true }), "performance");
  assert.equal(getSignalKind({ degradedStatusCount: 1 }), "health");
  assert.equal(
    getSignalKind({ degradedStatusCount: 1, isSlow: true }),
    "performance-and-health",
  );
  assert.equal(
    getSignalKind({ degradedStatusCount: 2, isSlow: true }),
    "performance-and-health",
  );
  assert.equal(getSignalKind({ failed: true }), "failure");

  assert.equal(getAttentionLevel({ durationBand: "within-threshold" }), "low");
  assert.equal(getAttentionLevel({ durationBand: "slow" }), "medium");
  assert.equal(
    getAttentionLevel({ durationBand: "within-threshold", degradedStatusCount: 1 }),
    "medium",
  );
  assert.equal(getAttentionLevel({ durationBand: "very-slow" }), "high");
  assert.equal(
    getAttentionLevel({ durationBand: "within-threshold", degradedStatusCount: 2 }),
    "high",
  );
  assert.equal(getAttentionLevel({ durationBand: "slow", failed: true }), "high");
  assert.equal(
    getAttentionLevel({ durationBand: "slow", degradedStatusCount: 1 }),
    "medium",
  );

  assert.equal(getSuggestedAction({}), "none");
  assert.equal(getSuggestedAction({ isSlow: true }), "inspect-slow-path");
  assert.equal(
    getSuggestedAction({ degradedStatusCount: 1 }),
    "inspect-degraded-dependencies",
  );
  assert.equal(
    getSuggestedAction({ degradedStatusCount: 1, isSlow: true }),
    "inspect-slow-and-degraded-dependencies",
  );
  assert.equal(
    getSuggestedAction({ degradedStatusCount: 2, isSlow: true }),
    "inspect-slow-and-degraded-dependencies",
  );
  assert.equal(getSuggestedAction({ failed: true }), "inspect-failure-cause");
});

test("route observability helper internals keep degraded extraction and outcome shape explicit", () => {
  assert.deepEqual(
    getDegradedStatusEntries({
      menuStatus: "fallback",
      productsStatus: "ok",
      shell: "fallback",
      cartStatus: "missing",
      itemCount: 3,
    }),
    [
      ["menuStatus", "fallback"],
      ["cartStatus", "missing"],
    ],
  );

  assert.deepEqual(getDegradedStatusEntries(), []);

  assert.deepEqual(
    buildObservedOutcomeDetail(40, 50, []),
    {
      durationBand: "within-threshold",
      healthState: "healthy",
      outcomeKind: "success",
      signalKind: "normal",
      attentionLevel: "low",
      suggestedAction: "none",
      degradedStatusCount: 0,
      degradedStatuses: undefined,
      degradedStatusKeys: undefined,
      degradedSurfaceCount: 0,
      degradedSurfaceKeys: undefined,
      degradedSurfaceFootprint: undefined,
      primaryDegradedStatusKey: undefined,
      primaryDegradedSurface: undefined,
    },
  );

  assert.deepEqual(
    buildObservedOutcomeDetail(60, 50, [["menuStatus", "fallback"]]),
    {
      durationBand: "slow",
      healthState: "degraded",
      outcomeKind: "slow-degraded-success",
      signalKind: "performance-and-health",
      attentionLevel: "medium",
      suggestedAction: "inspect-slow-and-degraded-dependencies",
      degradedStatusCount: 1,
      degradedStatuses: { menuStatus: "fallback" },
      degradedStatusKeys: ["menuStatus"],
      degradedSurfaceCount: 1,
      degradedSurfaceKeys: ["menu"],
      degradedSurfaceFootprint: "menu:fallback",
      primaryDegradedStatusKey: "menuStatus",
      primaryDegradedSurface: "menu",
    },
  );
});

test("route observability degraded log gate stays explicit across env modes", () => {
  const previousNodeEnv = process.env.NODE_ENV;
  const previousDegraded = process.env.DARWIN_WEB_LOG_DEGRADED;

  try {
    process.env.NODE_ENV = "test";
    delete process.env.DARWIN_WEB_LOG_DEGRADED;
    assert.equal(shouldLogDegradedOperations(), false);

    process.env.DARWIN_WEB_LOG_DEGRADED = "true";
    assert.equal(shouldLogDegradedOperations(), true);

    delete process.env.DARWIN_WEB_LOG_DEGRADED;
    process.env.NODE_ENV = "production";
    assert.equal(shouldLogDegradedOperations(), true);
  } finally {
    if (previousNodeEnv === undefined) {
      delete process.env.NODE_ENV;
    } else {
      process.env.NODE_ENV = previousNodeEnv;
    }

    if (previousDegraded === undefined) {
      delete process.env.DARWIN_WEB_LOG_DEGRADED;
    } else {
      process.env.DARWIN_WEB_LOG_DEGRADED = previousDegraded;
    }
  }
});

test("observeAsyncOperation reports slow operations above the threshold", async () => {
  const warnings: Array<Record<string, unknown>> = [];
  let tick = 100;

  const result = await observeAsyncOperation(
    {
      area: "home",
      operation: "compose",
      context: { culture: "de-DE", route: "/" },
      getSuccessDetail: (result) => ({ resultStatus: result }),
      thresholdMs: 50,
      now: () => {
        tick += 60;
        return tick;
      },
      warn: (_message, detail) => warnings.push(detail),
    },
    async () => "ok",
  );

  assert.equal(result, "ok");
  assert.equal(warnings.length, 1);
  assert.deepEqual(warnings[0], {
    area: "home",
    operation: "compose",
    operationKey: "home:compose",
    durationMs: warnings[0]?.durationMs,
    durationBand: "slow",
    healthState: "healthy",
    outcomeKind: "slow-success",
    signalKind: "performance",
    attentionLevel: "medium",
    suggestedAction: "inspect-slow-path",
    degradedStatusCount: 0,
    degradedStatuses: undefined,
    degradedStatusKeys: undefined,
    degradedSurfaceCount: 0,
    degradedSurfaceKeys: undefined,
    degradedSurfaceFootprint: undefined,
    primaryDegradedStatusKey: undefined,
    primaryDegradedSurface: undefined,
    culture: "de-DE",
    route: "/",
    resultStatus: "ok",
  });
});

test("observeAsyncOperation keeps healthy successes silent below the threshold", async () => {
  const warnings: Array<Record<string, unknown>> = [];
  let tick = 20;

  const result = await observeAsyncOperation(
    {
      area: "home",
      operation: "compose",
      context: { culture: "en-US", route: "/" },
      getSuccessDetail: () => ({
        resultStatus: "ok",
        menuItemCount: 4,
      }),
      thresholdMs: 100,
      now: () => {
        tick += 30;
        return tick;
      },
      warn: (_message, detail) => warnings.push(detail),
    },
    async () => "ok",
  );

  assert.equal(result, "ok");
  assert.deepEqual(warnings, []);
});

test("observeAsyncOperation reports failures with timing metadata", async () => {
  const failures: Array<Record<string, unknown>> = [];
  let tick = 10;

  await assert.rejects(
    observeAsyncOperation(
      {
        area: "checkout",
        operation: "load",
        context: { culture: "de-DE", route: "/checkout" },
        now: () => {
          tick += 25;
          return tick;
        },
        error: (_message, detail) => failures.push(detail),
      },
      async () => {
        throw new Error("boom");
      },
    ),
    /boom/,
  );

  assert.equal(failures.length, 1);
  assert.deepEqual(failures[0], {
    area: "checkout",
    operation: "load",
    operationKey: "checkout:load",
    durationMs: failures[0]?.durationMs,
    durationBand: "within-threshold",
    healthState: "failed",
    outcomeKind: "failure",
    signalKind: "failure",
    attentionLevel: "high",
    suggestedAction: "inspect-failure-cause",
    culture: "de-DE",
    route: "/checkout",
    cause: failures[0]?.cause,
  });
});

test("observeAsyncOperation reports degraded successful operations even when they are not slow", async () => {
  const warnings: Array<Record<string, unknown>> = [];
  let tick = 200;
  const previous = process.env.DARWIN_WEB_LOG_DEGRADED;
  process.env.DARWIN_WEB_LOG_DEGRADED = "true";

  try {
    const result = await observeAsyncOperation(
      {
        area: "shell",
        operation: "menu",
        context: { culture: "de-DE", route: "/" },
        getSuccessDetail: () => ({
          menuStatus: "fallback",
          menuItemCount: 3,
        }),
        thresholdMs: 500,
        now: () => {
          tick += 20;
          return tick;
        },
        warn: (_message, detail) => warnings.push(detail),
      },
      async () => "ok",
    );

    assert.equal(result, "ok");
    assert.equal(warnings.length, 1);
    assert.deepEqual(warnings[0], {
      area: "shell",
      operation: "menu",
      operationKey: "shell:menu",
      durationMs: warnings[0]?.durationMs,
      durationBand: "within-threshold",
      healthState: "degraded",
      outcomeKind: "degraded-success",
      signalKind: "health",
      attentionLevel: "medium",
      suggestedAction: "inspect-degraded-dependencies",
      degradedStatusCount: 1,
      degradedStatuses: { menuStatus: "fallback" },
      degradedStatusKeys: ["menuStatus"],
      degradedSurfaceCount: 1,
      degradedSurfaceKeys: ["menu"],
      degradedSurfaceFootprint: "menu:fallback",
      primaryDegradedStatusKey: "menuStatus",
      primaryDegradedSurface: "menu",
      culture: "de-DE",
      route: "/",
      menuStatus: "fallback",
      menuItemCount: 3,
    });
  } finally {
    if (previous === undefined) {
      delete process.env.DARWIN_WEB_LOG_DEGRADED;
    } else {
      process.env.DARWIN_WEB_LOG_DEGRADED = previous;
    }
  }
});

test("observeAsyncOperation keeps degraded successes silent when the degraded gate is off", async () => {
  const warnings: Array<Record<string, unknown>> = [];
  let tick = 500;
  const previousNodeEnv = process.env.NODE_ENV;
  const previousDegraded = process.env.DARWIN_WEB_LOG_DEGRADED;

  try {
    process.env.NODE_ENV = "test";
    delete process.env.DARWIN_WEB_LOG_DEGRADED;

    const result = await observeAsyncOperation(
      {
        area: "shell",
        operation: "menu",
        context: { culture: "de-DE", route: "/" },
        getSuccessDetail: () => ({
          menuStatus: "fallback",
          menuItemCount: 3,
        }),
        thresholdMs: 500,
        now: () => {
          tick += 20;
          return tick;
        },
        warn: (_message, detail) => warnings.push(detail),
      },
      async () => "ok",
    );

    assert.equal(result, "ok");
    assert.deepEqual(warnings, []);
  } finally {
    if (previousNodeEnv === undefined) {
      delete process.env.NODE_ENV;
    } else {
      process.env.NODE_ENV = previousNodeEnv;
    }

    if (previousDegraded === undefined) {
      delete process.env.DARWIN_WEB_LOG_DEGRADED;
    } else {
      process.env.DARWIN_WEB_LOG_DEGRADED = previousDegraded;
    }
  }
});

test("observeAsyncOperation distinguishes very slow degraded successes", async () => {
  const warnings: Array<Record<string, unknown>> = [];
  let tick = 300;

  const result = await observeAsyncOperation(
    {
      area: "catalog",
      operation: "route-context",
      context: { culture: "de-DE", route: "/catalog" },
      getSuccessDetail: () => ({
        productsStatus: "fallback",
        cmsPagesStatus: "fallback",
      }),
      thresholdMs: 50,
      now: () => {
        tick += 180;
        return tick;
      },
      warn: (_message, detail) => warnings.push(detail),
    },
    async () => "ok",
  );

  assert.equal(result, "ok");
  assert.equal(warnings.length, 1);
  assert.deepEqual(warnings[0], {
    area: "catalog",
    operation: "route-context",
    operationKey: "catalog:route-context",
    durationMs: warnings[0]?.durationMs,
    durationBand: "very-slow",
    healthState: "multi-degraded",
    outcomeKind: "slow-degraded-success",
    signalKind: "performance-and-health",
    attentionLevel: "high",
    suggestedAction: "inspect-slow-and-degraded-dependencies",
    degradedStatusCount: 2,
    degradedStatuses: {
      productsStatus: "fallback",
      cmsPagesStatus: "fallback",
    },
    degradedStatusKeys: ["productsStatus", "cmsPagesStatus"],
    degradedSurfaceCount: 2,
    degradedSurfaceKeys: ["products", "cmsPages"],
    degradedSurfaceFootprint: "products:fallback|cmsPages:fallback",
    primaryDegradedStatusKey: "productsStatus",
    primaryDegradedSurface: "products",
    culture: "de-DE",
    route: "/catalog",
    productsStatus: "fallback",
    cmsPagesStatus: "fallback",
  });
});









