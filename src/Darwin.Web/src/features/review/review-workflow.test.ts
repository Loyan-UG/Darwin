import assert from "node:assert/strict";
import test from "node:test";
import {
  buildPreferredCatalogReviewWindowHref,
  buildPreferredCmsReviewWindowHref,
  getPendingCatalogReviewQueueState,
  getPendingCmsReviewQueueState,
  getPreferredCatalogReviewState,
  getPreferredCmsReviewState,
} from "@/features/review/review-workflow";

test("getPreferredCmsReviewState prefers attention when attention pages dominate", () => {
  assert.equal(getPreferredCmsReviewState(1, 3), "needs-attention");
  assert.equal(getPreferredCmsReviewState(3, 1), "ready");
});

test("buildPreferredCmsReviewWindowHref keeps review context while forcing the correct lens", () => {
  assert.equal(
    buildPreferredCmsReviewWindowHref("needs-attention", {
      visibleQuery: "faq",
      visibleState: "all",
      visibleSort: "featured",
    }),
    "/cms?visibleQuery=faq&visibleState=needs-attention&visibleSort=attention-first",
  );
});

test("getPendingCmsReviewQueueState excludes the current page and limits the preview", () => {
  const state = getPendingCmsReviewQueueState(
    [
      { id: "1", slug: "a", title: "A", metaTitle: null, metaDescription: null },
      { id: "2", slug: "b", title: "B", metaTitle: "B", metaDescription: null },
      { id: "3", slug: "c", title: "C", metaTitle: null, metaDescription: "C" },
      { id: "4", slug: "d", title: "D", metaTitle: null, metaDescription: null },
    ],
    { currentSlug: "d", previewCount: 2 },
  );

  assert.equal(state.queue.length, 3);
  assert.equal(state.nextTarget?.page.slug, "a");
  assert.deepEqual(
    state.previewTargets.map((target) => target.page.slug),
    ["a", "b"],
  );
});

test("getPreferredCatalogReviewState prefers offers when visible offers dominate", () => {
  assert.equal(getPreferredCatalogReviewState(4, 2), "offers");
  assert.equal(getPreferredCatalogReviewState(1, 3), "base");
});

test("buildPreferredCatalogReviewWindowHref keeps category context while forcing the correct lens", () => {
  assert.equal(
    buildPreferredCatalogReviewWindowHref("offers", {
      category: "snacks",
      visibleQuery: "chips",
    }),
    "/catalog?category=snacks&visibleQuery=chips&visibleState=offers&visibleSort=offers-first",
  );
});

test("getPendingCatalogReviewQueueState excludes the current product and limits the preview", () => {
  const state = getPendingCatalogReviewQueueState(
    [
      {
        id: "1",
        slug: "a",
        name: "A",
        shortDescription: null,
        priceMinor: 100,
        compareAtPriceMinor: 150,
        currency: "EUR",
        primaryImageUrl: "",
      },
      {
        id: "2",
        slug: "b",
        name: "B",
        shortDescription: null,
        priceMinor: 100,
        compareAtPriceMinor: 130,
        currency: "EUR",
        primaryImageUrl: "/img-b.png",
      },
      {
        id: "3",
        slug: "c",
        name: "C",
        shortDescription: null,
        priceMinor: 100,
        compareAtPriceMinor: null,
        currency: "EUR",
        primaryImageUrl: "/img-c.png",
      },
    ],
    { currentSlug: "a", previewCount: 1 },
  );

  assert.equal(state.queue.length, 2);
  assert.equal(state.nextTarget?.product.slug, "b");
  assert.deepEqual(
    state.previewTargets.map((target) => target.product.slug),
    ["b"],
  );
});
