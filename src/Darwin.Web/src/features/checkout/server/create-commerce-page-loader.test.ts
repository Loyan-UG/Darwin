import assert from "node:assert/strict";
import test from "node:test";
import { createCommercePageLoaderCore } from "@/features/checkout/server/create-commerce-page-loader-core";

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
