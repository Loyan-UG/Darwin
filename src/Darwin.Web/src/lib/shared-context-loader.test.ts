import assert from "node:assert/strict";
import test from "node:test";
import {
  buildSharedContextLoaderObservationContext,
  buildSharedContextLoaderSuccessContext,
  createSharedContextLoader,
} from "@/lib/shared-context-loader";

test("shared-context loader helper builders keep canonical and raw diagnostics explicit", () => {
  assert.deepEqual(
    buildSharedContextLoaderObservationContext(
      "storefront-continuation",
      {
        culture: "de-DE",
        route: "/catalog",
      },
      {
        hasCanonicalNormalization: true,
      },
    ),
    {
      sharedContextKind: "storefront-continuation",
      sharedContextNormalization: "canonical",
      culture: "de-DE",
      route: "/catalog",
    },
  );

  assert.deepEqual(
    buildSharedContextLoaderSuccessContext(
      "member-summary",
      {
        culture: "en-US",
        scope: "orders-page",
        primaryStatus: "ok",
      },
    ),
    {
      sharedContextKind: "member-summary",
      sharedContextNormalization: "raw",
      culture: "en-US",
      scope: "orders-page",
      primaryStatus: "ok",
    },
  );

  assert.deepEqual(
    buildSharedContextLoaderObservationContext("public-storefront"),
    {
      sharedContextKind: "public-storefront",
      sharedContextNormalization: "raw",
    },
  );
});

test("createSharedContextLoader wraps context and success diagnostics with shared context metadata", async () => {
  let contextSnapshot: Record<string, unknown> | undefined;
  let successSnapshot: Record<string, unknown> | undefined;

  const loader = createSharedContextLoader({
    kind: "storefront-continuation",
    area: "unit-shared-context",
    operation: "load-continuation",
    normalizeArgs: (culture: string) => [culture.trim()] as [string],
    getContext: (culture: string) => {
      contextSnapshot = { culture };
      return { culture };
    },
    getSuccessContext: (result) => {
      successSnapshot = { cmsStatus: result.cmsStatus };
      return { cmsStatus: result.cmsStatus };
    },
    load: async (culture: string) => ({
      cmsStatus: culture === "de-DE" ? "ok" : "fallback",
    }),
  });

  const result = await loader(" de-DE ");

  assert.equal(result.cmsStatus, "ok");
  assert.deepEqual(contextSnapshot, { culture: "de-DE" });
  assert.deepEqual(successSnapshot, { cmsStatus: "ok" });
});

test("createSharedContextLoader keeps shared-context results stable per argument tuple", async () => {
  let executions = 0;
  const loader = createSharedContextLoader({
    kind: "public-storefront",
    area: "unit-shared-context",
    operation: "load-context",
    getContext: (culture: string) => ({ culture }),
    getSuccessContext: (result) => ({ status: result.status }),
    load: async (culture: string) => {
      executions += 1;
      return { status: culture, execution: executions };
    },
  });

  const [first, second, third] = await Promise.all([
    loader("de-DE"),
    loader("de-DE"),
    loader("en-US"),
  ]);

  assert.equal(first.status, "de-DE");
  assert.equal(second.status, "de-DE");
  assert.equal(third.status, "en-US");
  assert.equal(executions >= 2, true);
});

test("createSharedContextLoader normalizes equivalent arguments before caching", async () => {
  const loader = createSharedContextLoader({
    kind: "member-entry",
    area: "unit-shared-context",
    operation: "load-entry",
    normalizeArgs: (culture: string, route: string) =>
      [culture.trim(), route.trim()] as [string, string],
    getContext: (culture: string, route: string) => ({ culture, route }),
    getSuccessContext: (result) => ({
      sessionState: result.sessionState,
    }),
    load: async (culture: string, route: string) => ({
      sessionState: `${culture}:${route}`,
    }),
  });

  const [first, second] = await Promise.all([
    loader("de-DE", "/account"),
    loader(" de-DE ", " /account "),
  ]);

  assert.equal(first.sessionState, "de-DE:/account");
  assert.equal(second.sessionState, "de-DE:/account");
});

test("createSharedContextLoader keeps raw member-summary success context explicit", async () => {
  let successSnapshot: Record<string, unknown> | undefined;

  const loader = createSharedContextLoader({
    kind: "member-summary",
    area: "unit-shared-context",
    operation: "load-summary",
    getContext: (culture: string, scope: string) => ({ culture, scope }),
    getSuccessContext: (result, culture: string, scope: string) => {
      successSnapshot = {
        culture,
        scope,
        primaryStatus: result.primaryStatus,
      };
      return successSnapshot;
    },
    load: async (_culture: string, scope: string) => ({
      primaryStatus: scope === "orders-page" ? "ok" : "fallback",
    }),
  });

  const result = await loader("en-US", "orders-page");

  assert.equal(result.primaryStatus, "ok");
  assert.deepEqual(successSnapshot, {
    culture: "en-US",
    scope: "orders-page",
    primaryStatus: "ok",
  });
});
test("createSharedContextLoader keeps base diagnostics explicit when context hooks return undefined", async () => {
  const loader = createSharedContextLoader({
    kind: "public-storefront",
    area: "unit-shared-context",
    operation: "load-undefined-context",
    getContext: () => undefined,
    getSuccessContext: () => undefined,
    load: async () => ({
      status: "ok",
    }),
  });

  const result = await loader();

  assert.deepEqual(result, {
    status: "ok",
  });
});
test("createSharedContextLoader keeps empty observation context branches explicit", async () => {
  const loader = createSharedContextLoader({
    kind: "public-storefront",
    area: "unit-shared-context",
    operation: "load-empty-context",
    getContext: () => ({}),
    getSuccessContext: (result) => ({ status: result.status }),
    load: async () => ({
      status: "ok",
    }),
  });

  const result = await loader();

  assert.deepEqual(result, {
    status: "ok",
  });
});

test("createSharedContextLoader keeps undefined success-context branches explicit", async () => {
  const loader = createSharedContextLoader({
    kind: "public-storefront",
    area: "unit-shared-context",
    operation: "load-undefined-success-context",
    getContext: () => ({ culture: "de-DE" }),
    getSuccessContext: () => undefined,
    load: async () => ({
      status: "ok",
    }),
  });

  const result = await loader();

  assert.deepEqual(result, {
    status: "ok",
  });
});test("createSharedContextLoader keeps empty success-context branches explicit", async () => {
  const loader = createSharedContextLoader({
    kind: "public-storefront",
    area: "unit-shared-context",
    operation: "load-empty-success-context",
    getContext: () => ({ culture: "de-DE" }),
    getSuccessContext: () => ({}),
    load: async () => ({
      status: "ok",
    }),
  });

  const result = await loader();

  assert.deepEqual(result, {
    status: "ok",
  });
});


