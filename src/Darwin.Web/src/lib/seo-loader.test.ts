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

test("buildSeoLoaderObservationContext adds canonical seo-loader diagnostics", () => {
  assert.deepEqual(
    buildSeoLoaderObservationContext("catalog-seo", {
      culture: "de-DE",
      route: "/catalog",
    }),
    {
      pageLoaderKind: "seo-metadata",
      seoArea: "catalog-seo",
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
    }),
    {
      pageLoaderKind: "seo-metadata",
      seoArea: "cms-seo",
      indexability: "noindex",
      canonicalPath: "/cms/impressum",
      noIndex: true,
      languageAlternateCount: 1,
    },
  );
});
