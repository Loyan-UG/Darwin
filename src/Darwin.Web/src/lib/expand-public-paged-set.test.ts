import assert from "node:assert/strict";
import test from "node:test";
import {
  selectExpandedPublicPagedSet,
  shouldExpandPublicPagedSet,
} from "@/lib/expand-public-paged-set";

test("shouldExpandPublicPagedSet only expands healthy partial result windows", () => {
  assert.equal(
    shouldExpandPublicPagedSet({
      status: "ok",
      data: {
        items: [{ id: "1" }],
        total: 3,
      },
    }),
    true,
  );

  assert.equal(
    shouldExpandPublicPagedSet({
      status: "ok",
      data: {
        items: [{ id: "1" }, { id: "2" }],
        total: 2,
      },
    }),
    false,
  );

  assert.equal(
    shouldExpandPublicPagedSet({
      status: "network-error",
      data: null,
    }),
    false,
  );
});

test("selectExpandedPublicPagedSet keeps the initial result when the expanded retry degrades", () => {
  const initialResult = {
    status: "ok" as const,
    data: {
      items: [{ id: "1" }],
      total: 3,
    },
  };
  const expandedResult = {
    status: "http-error" as const,
    data: null,
  };

  assert.equal(
    selectExpandedPublicPagedSet(initialResult, expandedResult),
    initialResult,
  );
});

test("selectExpandedPublicPagedSet prefers the expanded result when it succeeds", () => {
  const initialResult = {
    status: "ok" as const,
    data: {
      items: [{ id: "1" }],
      total: 3,
    },
  };
  const expandedResult = {
    status: "ok" as const,
    data: {
      items: [{ id: "1" }, { id: "2" }, { id: "3" }],
      total: 3,
    },
  };

  assert.equal(
    selectExpandedPublicPagedSet(initialResult, expandedResult),
    expandedResult,
  );
});
