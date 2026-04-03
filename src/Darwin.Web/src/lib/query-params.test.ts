import test from "node:test";
import assert from "node:assert/strict";
import {
  buildQuerySuffix,
  cloneSearchParams,
  serializeQueryParams,
} from "@/lib/query-params";

test("serializeQueryParams omits empty values but keeps numbers and booleans", () => {
  const result = serializeQueryParams({
    search: "offer",
    page: 2,
    visible: true,
    empty: "",
    missing: undefined,
    nullable: null,
  });

  assert.equal(result, "search=offer&page=2&visible=true");
});

test("buildQuerySuffix returns an empty string when nothing is serializable", () => {
  const result = buildQuerySuffix({
    search: "",
    page: undefined,
    enabled: null,
  });

  assert.equal(result, "");
});

test("cloneSearchParams supports strings and URLSearchParams-compatible inputs", () => {
  const fromString = cloneSearchParams("page=2&visibleQuery=offers");
  assert.equal(fromString.get("page"), "2");
  assert.equal(fromString.get("visibleQuery"), "offers");

  const source = new URLSearchParams({
    category: "tea",
    page: "3",
  });
  const cloned = cloneSearchParams(source);

  source.set("page", "4");

  assert.equal(cloned.get("category"), "tea");
  assert.equal(cloned.get("page"), "3");
});
