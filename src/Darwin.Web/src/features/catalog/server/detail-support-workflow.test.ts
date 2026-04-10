import assert from "node:assert/strict";
import test from "node:test";
import { summarizeProductDetailSupportWorkflow } from "@/features/catalog/server/get-product-detail-context";
import { summarizeCmsDetailSupportWorkflow } from "@/features/cms/server/get-cms-page-detail-context";

test("summarizeProductDetailSupportWorkflow keeps related and review support readable", () => {
  assert.equal(
    summarizeProductDetailSupportWorkflow({
      relatedProductsResult: { status: "ok" },
      relatedProducts: [{ id: "one" }, { id: "two" }],
      reviewProductsResult: { status: "degraded" },
      reviewProducts: [{ id: "three" }],
    }),
    "related:ok:2|review:degraded:1",
  );

  assert.equal(
    summarizeProductDetailSupportWorkflow({
      relatedProductsResult: null,
      relatedProducts: [],
      reviewProductsResult: null,
      reviewProducts: [],
    }),
    "related:not-requested:0|review:not-requested:0",
  );
});

test("summarizeCmsDetailSupportWorkflow keeps seed and visible review state readable", () => {
  assert.equal(
    summarizeCmsDetailSupportWorkflow({
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
    summarizeCmsDetailSupportWorkflow({
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
