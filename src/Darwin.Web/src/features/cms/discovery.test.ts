import test from "node:test";
import assert from "node:assert/strict";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  buildCmsVisibleWindow,
  filterVisiblePages,
  getPendingCmsContentQualityTargets,
  readCmsMetadataFocus,
  isDiscoveryReadyPage,
  readCmsVisibleSort,
  readCmsVisibleState,
  summarizeCmsMetadataDebt,
  sortVisiblePages,
} from "@/features/cms/discovery";

function createPage(
  overrides: Partial<PublicPageSummary>,
): PublicPageSummary {
  return {
    id: overrides.id ?? "page-1",
    title: overrides.title ?? "Page",
    slug: overrides.slug ?? "page",
    metaTitle: overrides.metaTitle ?? null,
    metaDescription: overrides.metaDescription ?? null,
  };
}

test("readCmsVisibleState keeps only supported CMS lens values", () => {
  assert.equal(readCmsVisibleState("ready"), "ready");
  assert.equal(readCmsVisibleState("needs-attention"), "needs-attention");
  assert.equal(readCmsVisibleState("invalid"), "all");
});

test("readCmsVisibleSort keeps only supported CMS sort values", () => {
  assert.equal(readCmsVisibleSort("title-asc"), "title-asc");
  assert.equal(readCmsVisibleSort("ready-first"), "ready-first");
  assert.equal(readCmsVisibleSort("invalid"), "featured");
});

test("readCmsMetadataFocus keeps only supported metadata-focus values", () => {
  assert.equal(readCmsMetadataFocus("missing-title"), "missing-title");
  assert.equal(readCmsMetadataFocus("missing-both"), "missing-both");
  assert.equal(readCmsMetadataFocus("invalid"), "all");
});

test("isDiscoveryReadyPage requires both meta title and meta description", () => {
  assert.equal(
    isDiscoveryReadyPage(
      createPage({ metaTitle: "Meta", metaDescription: "Description" }),
    ),
    true,
  );
  assert.equal(
    isDiscoveryReadyPage(createPage({ metaTitle: "Meta", metaDescription: null })),
    false,
  );
});

test("filterVisiblePages applies query and discovery-state lens together", () => {
  const pages = [
    createPage({
      id: "ready",
      title: "Published Story",
      slug: "published-story",
      metaTitle: "Story meta",
      metaDescription: "Story description",
    }),
    createPage({
      id: "attention",
      title: "Draft Follow-up",
      slug: "draft-follow-up",
      metaTitle: null,
      metaDescription: "Missing title",
    }),
  ];

  assert.deepEqual(
    filterVisiblePages(pages, "ready").map((page) => page.id),
    ["ready"],
  );
  assert.deepEqual(
    filterVisiblePages(pages, "needs-attention").map((page) => page.id),
    ["attention"],
  );
  assert.deepEqual(
    filterVisiblePages(pages, "all", "draft").map((page) => page.id),
    ["attention"],
  );
  assert.deepEqual(
    filterVisiblePages(pages, "all", undefined, "missing-title").map(
      (page) => page.id,
    ),
    ["attention"],
  );
  assert.deepEqual(
    filterVisiblePages(pages, "all", undefined, "missing-description").map(
      (page) => page.id,
    ),
    [],
  );
});

test("sortVisiblePages prioritizes ready or attention pages inside the visible window", () => {
  const pages = [
    createPage({
      id: "b-ready",
      title: "Beta Ready",
      metaTitle: "Meta",
      metaDescription: "Description",
    }),
    createPage({
      id: "a-attention",
      title: "Alpha Attention",
      metaTitle: null,
      metaDescription: "Missing title",
    }),
    createPage({
      id: "c-ready",
      title: "Charlie Ready",
      metaTitle: "Meta",
      metaDescription: "Description",
    }),
  ];

  assert.deepEqual(
    sortVisiblePages(pages, "title-asc").map((page) => page.id),
    ["a-attention", "b-ready", "c-ready"],
  );
  assert.deepEqual(
    sortVisiblePages(pages, "ready-first").map((page) => page.id),
    ["b-ready", "c-ready", "a-attention"],
  );
  assert.deepEqual(
    sortVisiblePages(pages, "attention-first").map((page) => page.id),
    ["a-attention", "b-ready", "c-ready"],
  );
});

test("buildCmsVisibleWindow paginates the full matching page set after state and sort lenses", () => {
  const pages = [
    createPage({
      id: "b-ready",
      title: "Beta Ready",
      metaTitle: "Meta",
      metaDescription: "Description",
    }),
    createPage({
      id: "a-attention",
      title: "Alpha Attention",
      metaTitle: null,
      metaDescription: "Missing title",
    }),
    createPage({
      id: "c-ready",
      title: "Charlie Ready",
      metaTitle: "Meta",
      metaDescription: "Description",
    }),
  ];

  const window = buildCmsVisibleWindow(pages, {
    page: 2,
    pageSize: 1,
    visibleState: "ready",
    visibleSort: "title-asc",
  });

  assert.equal(window.total, 2);
  assert.equal(window.totalPages, 2);
  assert.equal(window.currentPage, 2);
  assert.deepEqual(window.items.map((page) => page.id), ["c-ready"]);
});

test("buildCmsVisibleWindow also applies metadata focus across the full matching set", () => {
  const pages = [
    createPage({
      id: "both",
      title: "Both Missing",
      metaTitle: null,
      metaDescription: null,
    }),
    createPage({
      id: "title",
      title: "Title Missing",
      metaTitle: null,
      metaDescription: "Description",
    }),
    createPage({
      id: "ready",
      title: "Ready",
      metaTitle: "Meta",
      metaDescription: "Description",
    }),
  ];

  const window = buildCmsVisibleWindow(pages, {
    page: 1,
    pageSize: 5,
    visibleState: "all",
    visibleSort: "title-asc",
    metadataFocus: "missing-both",
  });

  assert.equal(window.total, 1);
  assert.deepEqual(window.items.map((page) => page.id), ["both"]);
});

test("getPendingCmsContentQualityTargets prioritizes the strongest metadata debt first", () => {
  const pages = [
    createPage({
      id: "both",
      title: "Both Missing",
      slug: "both-missing",
      metaTitle: null,
      metaDescription: null,
    }),
    createPage({
      id: "title",
      title: "Title Missing",
      slug: "title-missing",
      metaTitle: null,
      metaDescription: "Description",
    }),
    createPage({
      id: "ready",
      title: "Ready",
      slug: "ready",
      metaTitle: "Meta",
      metaDescription: "Description",
    }),
  ];

  const targets = getPendingCmsContentQualityTargets(pages);

  assert.deepEqual(targets.map((target) => target.page.id), ["both", "title"]);
  assert.equal(targets[0]?.missingMetaTitle, true);
  assert.equal(targets[0]?.missingMetaDescription, true);
});

test("summarizeCmsMetadataDebt counts metadata debt across the matching set", () => {
  const pages = [
    createPage({
      id: "both",
      metaTitle: null,
      metaDescription: null,
    }),
    createPage({
      id: "title",
      metaTitle: null,
      metaDescription: "Description",
    }),
    createPage({
      id: "ready",
      metaTitle: "Meta",
      metaDescription: "Description",
    }),
  ];

  assert.deepEqual(summarizeCmsMetadataDebt(pages), {
    totalCount: 3,
    readyCount: 1,
    attentionCount: 2,
    missingMetaTitleCount: 2,
    missingMetaDescriptionCount: 1,
    missingBothCount: 1,
  });
});
