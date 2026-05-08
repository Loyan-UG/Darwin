import assert from "node:assert/strict";
import test from "node:test";
import {
  createCachedObservedLoader,
  createObservedLoader,
} from "@/lib/observed-loader";

test("createObservedLoader forwards area, operation, threshold, and context", async () => {
  const calls: Array<Record<string, unknown>> = [];
  const loader = createObservedLoader({
    area: "unit-loader",
    operation: "run",
    thresholdMs: 123,
    getContext: (culture: string, page: number) => ({ culture, page }),
    load: async (culture: string, page: number) => {
      calls.push({ culture, page });
      return `${culture}:${page}`;
    },
  });

  const result = await loader("de-DE", 2);

  assert.equal(result, "de-DE:2");
  assert.deepEqual(calls, [{ culture: "de-DE", page: 2 }]);
});

test("createCachedObservedLoader keeps argument-specific results stable", async () => {
  let executions = 0;
  const loader = createCachedObservedLoader({
    area: "unit-loader",
    operation: "run-cached",
    getContext: (culture: string, page: number) => ({ culture, page }),
    load: async (culture: string, page: number) => {
      executions += 1;
      return `${culture}:${page}:${executions}`;
    },
  });

  const [first, second, third] = await Promise.all([
    loader("de-DE", 1),
    loader("de-DE", 1),
    loader("en-US", 1),
  ]);

  assert.match(first, /^de-DE:1:/);
  assert.match(second, /^de-DE:1:/);
  assert.match(third, /^en-US:1:/);
  assert.equal(executions >= 2, true);
});

test("createObservedLoader normalizes arguments before context and load", async () => {
  const calls: Array<Record<string, unknown>> = [];
  const loader = createObservedLoader({
    area: "unit-loader",
    operation: "run-normalized",
    normalizeArgs: (culture: string, page: number, query?: string) => [
      culture,
      page > 0 ? page : 1,
      query?.trim() || undefined,
    ],
    getContext: (culture: string, page: number, query?: string) => ({
      culture,
      page,
      query: query ?? null,
    }),
    load: async (culture: string, page: number, query?: string) => {
      calls.push({ culture, page, query: query ?? null });
      return `${culture}:${page}:${query ?? "none"}`;
    },
  });

  const result = await loader("de-DE", 0, "  hello  ");

  assert.equal(result, "de-DE:1:hello");
  assert.deepEqual(calls, [{ culture: "de-DE", page: 1, query: "hello" }]);
});

test("createCachedObservedLoader reuses normalized argument tuples", async () => {
  const loader = createCachedObservedLoader({
    area: "unit-loader",
    operation: "run-cached-normalized",
    normalizeArgs: (culture: string, page: number, query?: string) => [
      culture,
      page > 0 ? page : 1,
      query?.trim() || undefined,
    ],
    load: async (culture: string, page: number, query?: string) =>
      `${culture}:${page}:${query ?? "none"}`,
  });

  const [first, second] = await Promise.all([
    loader("de-DE", 1, "hello"),
    loader("de-DE", 0, "  hello  "),
  ]);

  assert.equal(first, second);
  assert.equal(first, "de-DE:1:hello");
});

test("createObservedLoader passes normalized args into success context", async () => {
  const successCalls: Array<Record<string, unknown>> = [];
  const loader = createObservedLoader({
    area: "unit-loader",
    operation: "run-success-context",
    normalizeArgs: (culture: string, page: number, query?: string) => [
      culture.trim(),
      page > 0 ? page : 1,
      query?.trim() || undefined,
    ],
    getSuccessContext: (
      result: string,
      culture: string,
      page: number,
      query?: string,
    ) => {
      successCalls.push({
        result,
        culture,
        page,
        query: query ?? null,
      });
      return {
        result,
        culture,
        page,
        query: query ?? null,
      };
    },
    load: async (culture: string, page: number, query?: string) =>
      `${culture}:${page}:${query ?? "none"}`,
  });

  const result = await loader(" de-DE ", 0, "  hello  ");

  assert.equal(result, "de-DE:1:hello");
  assert.deepEqual(successCalls, [
    {
      result: "de-DE:1:hello",
      culture: "de-DE",
      page: 1,
      query: "hello",
    },
  ]);
});

test("createCachedObservedLoader keeps bare default loaders usable outside a React cache boundary", async () => {
  let executions = 0;
  const loader = createCachedObservedLoader({
    area: "unit-loader",
    operation: "run-bare",
    load: async (culture: string) => {
      executions += 1;
      return `${culture}:${executions}`;
    },
  });

  const [first, second] = await Promise.all([
    loader("de-DE"),
    loader("de-DE"),
  ]);

  assert.equal(first, "de-DE:1");
  assert.equal(second, "de-DE:2");
  assert.equal(executions, 2);
});

test("createObservedLoader keeps bare default loaders usable without extra hooks", async () => {
  let executions = 0;
  const loader = createObservedLoader({
    area: "unit-loader",
    operation: "run-bare-direct",
    load: async (culture: string) => {
      executions += 1;
      return `${culture}:${executions}`;
    },
  });

  const first = await loader("de-DE");
  const second = await loader("de-DE");

  assert.equal(first, "de-DE:1");
  assert.equal(second, "de-DE:2");
  assert.equal(executions, 2);
});
test("createObservedLoader keeps explicit undefined context hooks equivalent to bare defaults", async () => {
  let executions = 0;
  const loader = createObservedLoader({
    area: "unit-loader",
    operation: "run-undefined-hooks",
    getContext: () => undefined,
    getSuccessContext: () => undefined,
    load: async (culture: string) => {
      executions += 1;
      return `${culture}:${executions}`;
    },
  });

  const first = await loader("de-DE");
  const second = await loader("de-DE");

  assert.equal(first, "de-DE:1");
  assert.equal(second, "de-DE:2");
  assert.equal(executions, 2);
});
test("createCachedObservedLoader keeps explicit undefined context hooks usable outside a React cache boundary", async () => {
  let executions = 0;
  const loader = createCachedObservedLoader({
    area: "unit-loader",
    operation: "run-cached-undefined-hooks",
    getContext: () => undefined,
    getSuccessContext: () => undefined,
    load: async (culture: string) => {
      executions += 1;
      return `${culture}:${executions}`;
    },
  });

  const [first, second] = await Promise.all([
    loader("de-DE"),
    loader("de-DE"),
  ]);

  assert.equal(first, "de-DE:1");
  assert.equal(second, "de-DE:2");
  assert.equal(executions, 2);
});

test("createObservedLoader keeps undefined success-context hooks explicit when base context exists", async () => {
  let executions = 0;
  const loader = createObservedLoader({
    area: "unit-loader",
    operation: "run-undefined-success-context",
    getContext: () => ({ culture: "de-DE" }),
    getSuccessContext: () => undefined,
    load: async (culture: string) => {
      executions += 1;
      return `${culture}:${executions}`;
    },
  });

  const first = await loader("de-DE");
  const second = await loader("de-DE");

  assert.equal(first, "de-DE:1");
  assert.equal(second, "de-DE:2");
  assert.equal(executions, 2);
});

test("createCachedObservedLoader keeps undefined success-context hooks explicit outside a React cache boundary", async () => {
  let executions = 0;
  const loader = createCachedObservedLoader({
    area: "unit-loader",
    operation: "run-cached-undefined-success-context",
    getContext: () => ({ culture: "de-DE" }),
    getSuccessContext: () => undefined,
    load: async (culture: string) => {
      executions += 1;
      return `${culture}:${executions}`;
    },
  });

  const [first, second] = await Promise.all([
    loader("de-DE"),
    loader("de-DE"),
  ]);

  assert.equal(first, "de-DE:1");
  assert.equal(second, "de-DE:2");
  assert.equal(executions, 2);
});test("createObservedLoader keeps empty observation context hooks equivalent to explicit base defaults", async () => {
  let executions = 0;
  const loader = createObservedLoader({
    area: "unit-loader",
    operation: "run-empty-context",
    getContext: () => ({}),
    getSuccessContext: () => ({}),
    load: async (culture: string) => {
      executions += 1;
      return `${culture}:${executions}`;
    },
  });

  const first = await loader("de-DE");
  const second = await loader("de-DE");

  assert.equal(first, "de-DE:1");
  assert.equal(second, "de-DE:2");
  assert.equal(executions, 2);
});

test("createCachedObservedLoader keeps empty observation context hooks usable outside a React cache boundary", async () => {
  let executions = 0;
  const loader = createCachedObservedLoader({
    area: "unit-loader",
    operation: "run-cached-empty-context",
    getContext: () => ({}),
    getSuccessContext: () => ({}),
    load: async (culture: string) => {
      executions += 1;
      return `${culture}:${executions}`;
    },
  });

  const [first, second] = await Promise.all([
    loader("de-DE"),
    loader("de-DE"),
  ]);

  assert.equal(first, "de-DE:1");
  assert.equal(second, "de-DE:2");
  assert.equal(executions, 2);
});


