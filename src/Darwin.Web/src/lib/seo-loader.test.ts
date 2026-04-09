import assert from "node:assert/strict";
import test from "node:test";
import {
  buildSeoLoaderObservationContext,
  buildSeoLoaderSuccessContext,
  createCachedObservedSeoMetadataLoader,
} from "@/lib/seo-loader";

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
        canonicalPath: `/cms/${slug}`,
        noIndex: false,
        languageAlternates: {
          "de-DE": `/cms/${slug}`,
          "en-US": `/en-US/cms/${slug}`,
        },
      };
    },
  });

  const first = await loader("de-DE", "impressum");
  const second = await loader("de-DE", "impressum");
  const third = await loader("en-US", "imprint");

  assert.equal(first.canonicalPath, "/cms/impressum");
  assert.equal(second.canonicalPath, "/cms/impressum");
  assert.equal(third.canonicalPath, "/cms/imprint");
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
      canonicalPath: `/cms/${slug}`,
      noIndex: false,
      languageAlternates: {},
    }),
  });

  const [first, second] = await Promise.all([
    loader("de-DE", "impressum"),
    loader(" de-DE ", " impressum "),
  ]);

  assert.equal(first.canonicalPath, "/cms/impressum");
  assert.equal(second.canonicalPath, "/cms/impressum");
  assert.equal(String(first.metadata.title), "de-DE:impressum");
  assert.equal(String(second.metadata.title), "de-DE:impressum");
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
});

test("buildSeoLoaderSuccessContext classifies indexability alongside health data", () => {
  assert.deepEqual(
    buildSeoLoaderSuccessContext("cms-seo", {
      metadata: {
        title: "Impressum",
      },
      canonicalPath: "/cms/impressum",
      noIndex: true,
      languageAlternates: {
        "de-DE": "/cms/impressum",
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
      seoTargetFootprint: "noindex|/cms/impressum",
      languageAlternateState: "present",
      languageAlternateFootprint: "de-DE",
      seoAlternateDetailFootprint: "de-DE",
      seoAlternateSummaryFootprint: "alternates:1[de-DE]",
      seoSummaryFootprint: "noindex|alternates:1[de-DE]",
      canonicalPath: "/cms/impressum",
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
