import test from "node:test";
import assert from "node:assert/strict";
import {
  buildCatalogReviewTargetHref,
  buildCatalogReviewWindowHref,
  buildCmsReviewTargetHref,
  buildCmsReviewWindowHref,
} from "@/features/review/review-window";

test("CMS review helpers preserve explicit visible review context", () => {
  assert.equal(
    buildCmsReviewWindowHref(
      { visibleQuery: "faq", visibleState: "needs-attention", visibleSort: "attention-first" },
      undefined,
    ),
    "/cms?visibleQuery=faq&visibleState=needs-attention&visibleSort=attention-first",
  );

  assert.equal(
    buildCmsReviewTargetHref("faq", {
      visibleQuery: "faq",
      visibleState: "needs-attention",
      visibleSort: "attention-first",
    }),
    "/cms/faq?visibleQuery=faq&visibleState=needs-attention&visibleSort=attention-first",
  );
});

test("catalog review helpers preserve category and review lens context", () => {
  assert.equal(
    buildCatalogReviewWindowHref({
      category: "snacks",
      visibleQuery: "chips",
      visibleState: "offers",
      visibleSort: "offers-first",
    }),
    "/catalog?category=snacks&visibleQuery=chips&visibleState=offers&visibleSort=offers-first",
  );

  assert.equal(
    buildCatalogReviewTargetHref("sea-salt-chips", {
      category: "snacks",
      visibleQuery: "chips",
      visibleState: "offers",
      visibleSort: "offers-first",
    }),
    "/catalog/sea-salt-chips?category=snacks&visibleQuery=chips&visibleState=offers&visibleSort=offers-first",
  );
});

test("review helpers drop default lens values but keep overrides", () => {
  assert.equal(
    buildCmsReviewTargetHref("faq", {
      visibleState: "all",
      visibleSort: "featured",
    }),
    "/cms/faq",
  );

  assert.equal(
    buildCatalogReviewWindowHref(
      {
        category: "snacks",
        visibleState: "all",
        visibleSort: "featured",
      },
      { visibleState: "base", visibleSort: "base-first" },
    ),
    "/catalog?category=snacks&visibleState=base&visibleSort=base-first",
  );
});
