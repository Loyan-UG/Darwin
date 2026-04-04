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
      {
        visibleQuery: "faq",
        visibleState: "needs-attention",
        visibleSort: "attention-first",
        metadataFocus: "missing-both",
      },
      undefined,
    ),
    "/cms?visibleQuery=faq&visibleState=needs-attention&visibleSort=attention-first&metadataFocus=missing-both",
  );

  assert.equal(
    buildCmsReviewTargetHref("faq", {
      visibleQuery: "faq",
      visibleState: "needs-attention",
      visibleSort: "attention-first",
      metadataFocus: "missing-both",
    }),
    "/cms/faq?visibleQuery=faq&visibleState=needs-attention&visibleSort=attention-first&metadataFocus=missing-both",
  );
});

test("catalog review helpers preserve category and review lens context", () => {
  assert.equal(
    buildCatalogReviewWindowHref({
      category: "snacks",
      visibleQuery: "chips",
      visibleState: "offers",
      visibleSort: "offers-first",
      mediaState: "missing-image",
      savingsBand: "hero",
    }),
    "/catalog?category=snacks&visibleQuery=chips&visibleState=offers&visibleSort=offers-first&mediaState=missing-image&savingsBand=hero",
  );

  assert.equal(
    buildCatalogReviewTargetHref("sea-salt-chips", {
      category: "snacks",
      visibleQuery: "chips",
      visibleState: "offers",
      visibleSort: "offers-first",
      mediaState: "missing-image",
      savingsBand: "hero",
    }),
    "/catalog/sea-salt-chips?category=snacks&visibleQuery=chips&visibleState=offers&visibleSort=offers-first&mediaState=missing-image&savingsBand=hero",
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
