import assert from "node:assert/strict";
import test from "node:test";
import { summarizeProductDetailContextFootprint } from "@/features/catalog/server/get-product-detail-context";
import { summarizeCmsDetailContextFootprint } from "@/features/cms/server/get-cms-page-detail-context";

test("summarizeProductDetailContextFootprint keeps related product state compact", () => {
  assert.equal(
    summarizeProductDetailContextFootprint({
      relatedProductsResult: { status: "ok" },
      relatedProducts: [{ id: "one" }, { id: "two" }],
      recommendedProductsResult: { status: "degraded" },
      recommendedProducts: [{ id: "three" }],
    }),
    "related:ok:2|recommended:degraded:1",
  );

  assert.equal(
    summarizeProductDetailContextFootprint({
      relatedProductsResult: null,
      relatedProducts: [],
      recommendedProductsResult: null,
      recommendedProducts: [],
    }),
    "related:not-requested:0|recommended:not-requested:0",
  );
});

test("summarizeCmsDetailContextFootprint keeps related content state compact", () => {
  assert.equal(
    summarizeCmsDetailContextFootprint({
      relatedPagesSeed: {
        status: "ok",
        data: {
          items: [{ id: "one" }, { id: "two" }, { id: "three" }],
        },
      },
      relatedPagesResult: { status: "ok" },
      relatedPages: [{ id: "one" }, { id: "two" }],
    }),
    "seed:ok:3|visible:ok:2",
  );

  assert.equal(
    summarizeCmsDetailContextFootprint({
      relatedPagesSeed: {
        status: "degraded",
        data: null,
      },
      relatedPagesResult: null,
      relatedPages: [],
    }),
    "seed:degraded:0|visible:degraded:0",
  );
});
