import assert from "node:assert/strict";
import test from "node:test";
import { summarizeCatalogIndexContextFootprint } from "@/features/catalog/server/get-catalog-page-context";
import { summarizeCmsIndexContextFootprint } from "@/features/cms/server/get-cms-page-context";

test("summarizeCatalogIndexContextFootprint keeps catalog context state compact", () => {
  assert.equal(
    summarizeCatalogIndexContextFootprint({
      cmsPagesResult: { status: "ok", data: { items: [{ id: "page-1" }] } },
      productsResult: { status: "ok", data: { items: [{ id: "product-1" }, { id: "product-2" }] } },
      cartSummary: { status: "ok" },
    }),
    "cms:ok:1|products:ok:2|cart:ok",
  );

  assert.equal(
    summarizeCatalogIndexContextFootprint({
      cmsPagesResult: null,
      productsResult: null,
      cartSummary: null,
    }),
    "cms:unknown:0|products:unknown:0|cart:missing",
  );
});

test("summarizeCmsIndexContextFootprint keeps help context state compact", () => {
  assert.equal(
    summarizeCmsIndexContextFootprint({
      categoriesResult: { status: "degraded", data: { items: [{ id: "category-1" }] } },
      productsResult: { status: "ok", data: { items: [{ id: "product-1" }, { id: "product-2" }, { id: "product-3" }] } },
      cartSummary: { status: "not-found" },
    }),
    "categories:degraded:1|products:ok:3|cart:not-found",
  );

  assert.equal(
    summarizeCmsIndexContextFootprint({
      categoriesResult: null,
      productsResult: null,
      cartSummary: null,
    }),
    "categories:unknown:0|products:unknown:0|cart:missing",
  );
});
