import assert from "node:assert/strict";
import test from "node:test";
import {
  buildSeoLoaderObservationContext,
  buildSeoLoaderSuccessContext,
  createCachedObservedSeoMetadataLoader,
} from "@/lib/seo-loader";
import { getSeoLoaderNormalizationMode } from "@/lib/seo-loader-diagnostics";

test("getSeoLoaderNormalizationMode keeps canonical versus raw explicit", () => {
  assert.equal(getSeoLoaderNormalizationMode(true), "canonical");
  assert.equal(getSeoLoaderNormalizationMode(false), "raw");
  assert.equal(getSeoLoaderNormalizationMode(undefined), "raw");
});

test("createCachedObservedSeoMetadataLoader keeps metadata payloads stable per argument tuple", async () => {
  let executions = 0;

  const loader = createCachedObservedSeoMetadataLoader({
    area: "unit-seo",
    operation: "load-metadata",
    getContext: (culture: string, slug: string) => ({ culture, slug }),
    load: async (culture: string, slug: string) => {
      executions += 1;

      return {
        metadata: {
          title: `${culture}:${slug}:${executions}`,
        },
        canonicalPath: `/page/${slug}`,
        noIndex: false,
        languageAlternates: {
          "de-DE": `/page/${slug}`,
          "en-US": `/en-US/page/${slug}`,
        },
      };
    },
  });

  const first = await loader("de-DE", "impressum");
  const second = await loader("de-DE", "impressum");
  const third = await loader("en-US", "imprint");

  assert.equal(first.canonicalPath, "/page/impressum");
  assert.equal(second.canonicalPath, "/page/impressum");
  assert.equal(third.canonicalPath, "/page/imprint");
  assert.equal(first.noIndex, false);
  assert.equal(third.noIndex, false);
  assert.equal(executions >= 2, true);
  assert.deepEqual(first.languageAlternates, second.languageAlternates);
  assert.notEqual(
    String(first.metadata.title),
    String(third.metadata.title),
  );
});

test("createCachedObservedSeoMetadataLoader preserves no-index and alternate payloads", async () => {
  const loader = createCachedObservedSeoMetadataLoader({
    area: "unit-seo",
    operation: "load-metadata",
    getContext: (culture: string, page: string) => ({ culture, page }),
    load: async (culture: string, page: string) => ({
      metadata: {
        title: `${culture}:${page}`,
      },
      canonicalPath: `/catalog/${page}`,
      noIndex: true,
      languageAlternates: {},
    }),
  });

  const result = await loader("de-DE", "angebote");

  assert.equal(result.canonicalPath, "/catalog/angebote");
  assert.equal(result.noIndex, true);
  assert.deepEqual(result.languageAlternates, {});
});

test("createCachedObservedSeoMetadataLoader normalizes equivalent arguments before caching", async () => {
  const loader = createCachedObservedSeoMetadataLoader({
    area: "unit-seo",
    operation: "load-metadata",
    normalizeArgs: (culture: string, slug: string) =>
      [culture.trim(), slug.trim()] as [string, string],
    getContext: (culture: string, slug: string) => ({ culture, slug }),
    load: async (culture: string, slug: string) => ({
      metadata: {
        title: `${culture}:${slug}`,
      },
      canonicalPath: `/page/${slug}`,
      noIndex: false,
      languageAlternates: {},
    }),
  });

  const [first, second] = await Promise.all([
    loader("de-DE", "impressum"),
    loader(" de-DE ", " impressum "),
  ]);

  assert.equal(first.canonicalPath, "/page/impressum");
  assert.equal(second.canonicalPath, "/page/impressum");
  assert.equal(String(first.metadata.title), "de-DE:impressum");
  assert.equal(String(second.metadata.title), "de-DE:impressum");
});

test("createCachedObservedSeoMetadataLoader feeds normalized args into observation context", async () => {
  let contextSnapshot: Record<string, unknown> | undefined;

  const loader = createCachedObservedSeoMetadataLoader({
    area: "unit-seo",
    operation: "load-metadata",
    normalizeArgs: (culture: string, slug: string) =>
      [culture.trim(), slug.trim()] as [string, string],
    getContext: (culture: string, slug: string) => {
      contextSnapshot = { culture, slug };
      return { culture, slug };
    },
    load: async (culture: string, slug: string) => ({
      metadata: {
        title: `${culture}:${slug}`,
      },
      canonicalPath: `/page/${slug}`,
      noIndex: false,
      languageAlternates: {
        "de-DE": `/page/${slug}`,
        "en-US": `/en-US/page/${slug}`,
      },
    }),
  });

  const result = await loader(" de-DE ", " impressum ");

  assert.equal(result.canonicalPath, "/page/impressum");
  assert.deepEqual(contextSnapshot, {
    culture: "de-DE",
    slug: "impressum",
  });
});

test("buildSeoLoaderObservationContext adds canonical seo-loader diagnostics", () => {
  assert.deepEqual(
    buildSeoLoaderObservationContext("catalog-seo", {
      culture: "de-DE",
      route: "/catalog",
    }, {
      hasCanonicalNormalization: true,
    }),
    {
      pageLoaderKind: "seo-metadata",
      seoArea: "catalog-seo",
      seoNormalization: "canonical",
      culture: "de-DE",
      route: "/catalog",
    },
  );

  assert.deepEqual(
    buildSeoLoaderObservationContext("catalog-seo", {
      culture: "en-US",
      route: "/en-US/catalog",
    }),
    {
      pageLoaderKind: "seo-metadata",
      seoArea: "catalog-seo",
      seoNormalization: "raw",
      culture: "en-US",
      route: "/en-US/catalog",
    },
  );

  assert.deepEqual(buildSeoLoaderObservationContext("catalog-seo"), {
    pageLoaderKind: "seo-metadata",
    seoArea: "catalog-seo",
    seoNormalization: "raw",
  });
});

test("buildSeoLoaderSuccessContext classifies indexability alongside health data", () => {
  assert.deepEqual(
    buildSeoLoaderSuccessContext("cms-seo", {
      metadata: {
        title: "Impressum",
      },
      canonicalPath: "/page/impressum",
      noIndex: true,
      languageAlternates: {
        "de-DE": "/page/impressum",
      },
    }, {
      hasCanonicalNormalization: true,
    }),
    {
      pageLoaderKind: "seo-metadata",
      seoArea: "cms-seo",
      seoNormalization: "canonical",
      indexability: "noindex",
      seoIndexability: "noindex",
      seoMetadataState: "private",
      seoVisibilityFootprint: "noindex|private",
      seoTargetFootprint: "noindex|/page/impressum",
      languageAlternateState: "present",
      languageAlternateFootprint: "de-DE",
      seoAlternateDetailFootprint: "de-DE",
      seoAlternateSummaryFootprint: "alternates:1[de-DE]",
      seoSummaryFootprint: "noindex|alternates:1[de-DE]",
      canonicalPath: "/page/impressum",
      noIndex: true,
      languageAlternateCount: 1,
    },
  );
});

test("buildSeoLoaderSuccessContext keeps alternate footprint culture-stable", () => {
  const result = buildSeoLoaderSuccessContext("catalog-seo", {
    metadata: {
      title: "Product",
    },
    canonicalPath: "/catalog/product",
    noIndex: false,
    languageAlternates: {
      "en-US": "/en-US/catalog/product",
      "x-default": "/catalog/produkt",
      "de-DE": "/catalog/produkt",
    },
  });

  assert.equal(result.languageAlternateFootprint, "x-default|de-DE|en-US");
  assert.equal(result.seoSummaryFootprint, "indexable|alternates:3[x-default|de-DE|en-US]");
});

test("buildSeoLoaderSuccessContext keeps raw seo diagnostics explicit", () => {
  assert.deepEqual(
    buildSeoLoaderSuccessContext("catalog-seo", {
      metadata: {
        title: "Catalog",
      },
      canonicalPath: "/en-US/catalog",
      noIndex: false,
      languageAlternates: {
        "de-DE": "/catalog",
        "en-US": "/en-US/catalog",
      },
    }),
    {
      pageLoaderKind: "seo-metadata",
      seoArea: "catalog-seo",
      seoNormalization: "raw",
      indexability: "indexable",
      seoIndexability: "indexable",
      seoMetadataState: "localized",
      seoVisibilityFootprint: "indexable|localized",
      seoTargetFootprint: "indexable|/en-US/catalog",
      languageAlternateState: "present",
      languageAlternateFootprint: "de-DE|en-US",
      seoAlternateDetailFootprint: "de-DE|en-US",
      seoAlternateSummaryFootprint: "alternates:2[de-DE|en-US]",
      seoSummaryFootprint: "indexable|alternates:2[de-DE|en-US]",
      canonicalPath: "/en-US/catalog",
      noIndex: false,
      languageAlternateCount: 2,
    },
  );
});

test("createCachedObservedSeoMetadataLoader keeps undefined observation context branches explicit", async () => {
  const loader = createCachedObservedSeoMetadataLoader({
    area: "unit-seo",
    operation: "load-undefined-context",
    getContext: () => undefined,
    load: async (culture: string, slug: string) => ({
      metadata: {
        title: `${culture}:${slug}`,
      },
      canonicalPath: `/page/${slug}`,
      noIndex: false,
      languageAlternates: {},
    }),
  });

  const result = await loader("de-DE", "faq");

  assert.equal(result.canonicalPath, "/page/faq");
  assert.equal(result.noIndex, false);
  assert.deepEqual(result.languageAlternates, {});
});test("createCachedObservedSeoMetadataLoader keeps empty observation context branches explicit", async () => {
  const loader = createCachedObservedSeoMetadataLoader({
    area: "unit-seo",
    operation: "load-empty-context",
    getContext: () => ({}),
    load: async (culture: string, slug: string) => ({
      metadata: {
        title: `${culture}:${slug}`,
      },
      canonicalPath: `/page/${slug}`,
      noIndex: false,
      languageAlternates: {},
    }),
  });

  const result = await loader("de-DE", "faq");

  assert.equal(result.canonicalPath, "/page/faq");
  assert.equal(result.noIndex, false);
  assert.deepEqual(result.languageAlternates, {});
});
test("buildSeoLoaderSuccessContext keeps raw single-locale seo diagnostics explicit", () => {
  assert.deepEqual(
    buildSeoLoaderSuccessContext("catalog-seo", {
      metadata: {
        title: "Catalog",
      },
      canonicalPath: "/en-US/catalog",
      noIndex: false,
    }),
    {
      pageLoaderKind: "seo-metadata",
      seoArea: "catalog-seo",
      seoNormalization: "raw",
      indexability: "indexable",
      seoIndexability: "indexable",
      seoMetadataState: "single-locale",
      seoVisibilityFootprint: "indexable|single-locale",
      seoTargetFootprint: "indexable|/en-US/catalog",
      languageAlternateState: "missing",
      languageAlternateFootprint: "none",
      seoAlternateDetailFootprint: "none",
      seoAlternateSummaryFootprint: "alternates:none",
      seoSummaryFootprint: "indexable|alternates:0[none]",
      canonicalPath: "/en-US/catalog",
      noIndex: false,
      languageAlternateCount: 0,
    },
  );
});


