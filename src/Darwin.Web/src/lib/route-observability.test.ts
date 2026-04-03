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
});

test("observeAsyncOperation reports failures with timing metadata", async () => {
  const failures: Array<Record<string, unknown>> = [];
  let tick = 10;

  await assert.rejects(
    observeAsyncOperation(
      {
        area: "checkout",
        operation: "load",
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
});
