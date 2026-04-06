import test from "node:test";
import assert from "node:assert/strict";
import { observeAsyncOperation } from "@/lib/route-observability";

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
  assert.equal(warnings[0]?.area, "home");
  assert.equal(warnings[0]?.operation, "compose");
  assert.equal(warnings[0]?.culture, "de-DE");
  assert.equal(warnings[0]?.route, "/");
  assert.equal(warnings[0]?.resultStatus, "ok");
  assert.equal(warnings[0]?.operationKey, "home:compose");
  assert.equal(warnings[0]?.outcomeKind, "slow-success");
  assert.equal(warnings[0]?.signalKind, "performance");
  assert.equal(warnings[0]?.attentionLevel, "medium");
  assert.equal(warnings[0]?.suggestedAction, "inspect-slow-path");
  assert.equal(warnings[0]?.healthState, "healthy");
  assert.equal(warnings[0]?.durationBand, "slow");
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
  assert.equal(failures[0]?.area, "checkout");
  assert.equal(failures[0]?.operation, "load");
  assert.equal(failures[0]?.culture, "de-DE");
  assert.equal(failures[0]?.route, "/checkout");
  assert.equal(failures[0]?.operationKey, "checkout:load");
  assert.equal(failures[0]?.outcomeKind, "failure");
  assert.equal(failures[0]?.signalKind, "failure");
  assert.equal(failures[0]?.attentionLevel, "high");
  assert.equal(failures[0]?.suggestedAction, "inspect-failure-cause");
  assert.equal(failures[0]?.healthState, "failed");
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
    assert.equal(warnings[0]?.area, "shell");
    assert.equal(warnings[0]?.operation, "menu");
    assert.equal(warnings[0]?.menuStatus, "fallback");
    assert.equal(warnings[0]?.operationKey, "shell:menu");
    assert.equal(warnings[0]?.outcomeKind, "degraded-success");
    assert.equal(warnings[0]?.signalKind, "health");
    assert.equal(warnings[0]?.attentionLevel, "medium");
    assert.equal(warnings[0]?.suggestedAction, "inspect-degraded-dependencies");
    assert.equal(warnings[0]?.healthState, "degraded");
    assert.equal(warnings[0]?.durationBand, "within-threshold");
    assert.equal(warnings[0]?.degradedStatusCount, 1);
    assert.deepEqual(warnings[0]?.degradedStatuses, { menuStatus: "fallback" });
    assert.deepEqual(warnings[0]?.degradedStatusKeys, ["menuStatus"]);
    assert.equal(warnings[0]?.primaryDegradedStatusKey, "menuStatus");
  } finally {
    if (previous === undefined) {
      delete process.env.DARWIN_WEB_LOG_DEGRADED;
    } else {
      process.env.DARWIN_WEB_LOG_DEGRADED = previous;
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
  assert.equal(warnings[0]?.outcomeKind, "slow-degraded-success");
  assert.equal(warnings[0]?.operationKey, "catalog:route-context");
  assert.equal(warnings[0]?.signalKind, "performance-and-health");
  assert.equal(warnings[0]?.attentionLevel, "high");
  assert.equal(
    warnings[0]?.suggestedAction,
    "inspect-slow-and-degraded-dependencies",
  );
  assert.equal(warnings[0]?.healthState, "multi-degraded");
  assert.equal(warnings[0]?.durationBand, "very-slow");
  assert.equal(warnings[0]?.degradedStatusCount, 2);
  assert.deepEqual(warnings[0]?.degradedStatuses, {
    productsStatus: "fallback",
    cmsPagesStatus: "fallback",
  });
  assert.deepEqual(warnings[0]?.degradedStatusKeys, [
    "productsStatus",
    "cmsPagesStatus",
  ]);
  assert.equal(warnings[0]?.primaryDegradedStatusKey, "productsStatus");
});
