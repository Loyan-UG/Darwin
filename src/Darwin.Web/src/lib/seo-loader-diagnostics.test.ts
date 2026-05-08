import assert from "node:assert/strict";
import test from "node:test";
import {
  buildSeoIndexability,
  buildSeoLanguageAlternateDetailFootprint,
  buildSeoLanguageAlternateFootprint,
  buildSeoLanguageAlternateState,
  buildSeoLoaderBaseDiagnostics,
  buildSeoMetadataState,
  buildSeoSuccessDiagnostics,
  buildSeoSummaryFootprint,
  buildSeoTargetFootprint,
  buildSeoVisibilityFootprint,
} from "@/lib/seo-loader-diagnostics";

test("buildSeoLanguageAlternateFootprint keeps x-default first and cultures stable", () => {
  assert.equal(
    buildSeoLanguageAlternateFootprint({
      "en-US": "/en-US/catalog/product",
      "x-default": "/catalog/produkt",
      "de-DE": "/catalog/produkt",
    }),
    "alternates:3[x-default|de-DE|en-US]",
  );

  assert.equal(buildSeoLanguageAlternateFootprint({}), "alternates:none");
});

test("buildSeoLanguageAlternateDetailFootprint keeps x-default first and cultures stable", () => {
  assert.equal(
    buildSeoLanguageAlternateDetailFootprint({
      "en-US": "/en-US/catalog/product",
      "x-default": "/catalog/produkt",
      "de-DE": "/catalog/produkt",
    }),
    "x-default|de-DE|en-US",
  );

  assert.equal(buildSeoLanguageAlternateDetailFootprint({}), "none");
});

test("buildSeoVisibilityFootprint classifies localized, single-locale, and private metadata", () => {
  assert.equal(
    buildSeoVisibilityFootprint({
      noIndex: false,
      languageAlternates: {
        "de-DE": "/page/impressum",
        "en-US": "/en-US/page/imprint",
      },
    }),
    "indexable|localized",
  );

  assert.equal(
    buildSeoVisibilityFootprint({
      noIndex: false,
    }),
    "indexable|single-locale",
  );

  assert.equal(
    buildSeoVisibilityFootprint({
      noIndex: true,
    }),
    "noindex|private",
  );
});

test("buildSeoMetadataState classifies localized, single-locale, and private metadata", () => {
  assert.equal(
    buildSeoMetadataState({
      noIndex: false,
      languageAlternates: {
        "de-DE": "/page/impressum",
        "en-US": "/en-US/page/imprint",
      },
    }),
    "localized",
  );

  assert.equal(
    buildSeoMetadataState({
      noIndex: false,
    }),
    "single-locale",
  );

  assert.equal(
    buildSeoMetadataState({
      noIndex: true,
    }),
    "private",
  );
});

test("buildSeoIndexability classifies public and private metadata visibility", () => {
  assert.equal(
    buildSeoIndexability({
      noIndex: false,
    }),
    "indexable",
  );

  assert.equal(
    buildSeoIndexability({
      noIndex: true,
    }),
    "noindex",
  );
});

test("buildSeoLanguageAlternateState distinguishes present and missing alternates", () => {
  assert.equal(
    buildSeoLanguageAlternateState({
      "de-DE": "/page/impressum",
    }),
    "present",
  );

  assert.equal(buildSeoLanguageAlternateState({}), "missing");
  assert.equal(buildSeoLanguageAlternateState(), "missing");
});

test("buildSeoSummaryFootprint keeps canonical alternate rollups stable", () => {
  assert.equal(
    buildSeoSummaryFootprint({
      noIndex: false,
      languageAlternates: {
        "en-US": "/en-US/catalog/product",
        "x-default": "/catalog/produkt",
        "de-DE": "/catalog/produkt",
      },
    }),
    "indexable|alternates:3[x-default|de-DE|en-US]",
  );

  assert.equal(
    buildSeoSummaryFootprint({
      noIndex: true,
    }),
    "noindex|alternates:0[none]",
  );
});

test("buildSeoTargetFootprint keeps canonical target classification stable", () => {
  assert.equal(
    buildSeoTargetFootprint({
      canonicalPath: "/page/impressum",
      noIndex: false,
      languageAlternates: {
        "de-DE": "/page/impressum",
        "en-US": "/en-US/page/imprint",
      },
    }),
    "indexable|/page/impressum",
  );

  assert.equal(
    buildSeoTargetFootprint({
      canonicalPath: "/account/sign-in",
      noIndex: true,
    }),
    "noindex|/account/sign-in",
  );
});

test("buildSeoLoaderBaseDiagnostics keeps area and normalization mode explicit", () => {
  assert.deepEqual(
    buildSeoLoaderBaseDiagnostics("catalog-seo", {
      hasCanonicalNormalization: true,
      extras: {
        culture: "de-DE",
        route: "/catalog",
      },
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
    buildSeoLoaderBaseDiagnostics("catalog-seo", {
      extras: {
        culture: "en-US",
        route: "/en-US/catalog",
      },
    }),
    {
      pageLoaderKind: "seo-metadata",
      seoArea: "catalog-seo",
      seoNormalization: "raw",
      culture: "en-US",
      route: "/en-US/catalog",
    },
  );

  assert.deepEqual(buildSeoLoaderBaseDiagnostics("catalog-seo"), {
    pageLoaderKind: "seo-metadata",
    seoArea: "catalog-seo",
    seoNormalization: "raw",
  });
});

test("buildSeoSuccessDiagnostics keeps indexability and alternate footprint aligned", () => {
  assert.deepEqual(
    buildSeoSuccessDiagnostics(
      "cms-seo",
      {
        metadata: {
          title: "Impressum",
        },
        canonicalPath: "/page/impressum",
        noIndex: false,
        languageAlternates: {
          "en-US": "/en-US/page/imprint",
          "de-DE": "/page/impressum",
        },
      },
      {
        hasCanonicalNormalization: true,
      },
    ),
    {
      pageLoaderKind: "seo-metadata",
      seoArea: "cms-seo",
      seoNormalization: "canonical",
      indexability: "indexable",
      seoMetadataState: "localized",
      seoVisibilityFootprint: "indexable|localized",
      seoTargetFootprint: "indexable|/page/impressum",
      languageAlternateState: "present",
      languageAlternateFootprint: "de-DE|en-US",
      seoAlternateSummaryFootprint: "alternates:2[de-DE|en-US]",
      seoSummaryFootprint: "indexable|alternates:2[de-DE|en-US]",
    },
  );
});

test("buildSeoSuccessDiagnostics keeps noindex and missing alternates explicit", () => {
  assert.deepEqual(
    buildSeoSuccessDiagnostics(
      "account-seo",
      {
        metadata: {
          title: "Account",
        },
        canonicalPath: "/account/sign-in",
        noIndex: true,
      },
      {
        hasCanonicalNormalization: true,
      },
    ),
    {
      pageLoaderKind: "seo-metadata",
      seoArea: "account-seo",
      seoNormalization: "canonical",
      indexability: "noindex",
      seoMetadataState: "private",
      seoVisibilityFootprint: "noindex|private",
      seoTargetFootprint: "noindex|/account/sign-in",
      languageAlternateState: "missing",
      languageAlternateFootprint: "none",
      seoAlternateSummaryFootprint: "alternates:none",
      seoSummaryFootprint: "noindex|alternates:0[none]",
    },
  );
});

test("buildSeoSuccessDiagnostics keeps single-locale public metadata explicit", () => {
  assert.deepEqual(
    buildSeoSuccessDiagnostics(
      "catalog-seo",
      {
        metadata: {
          title: "Catalog",
        },
        canonicalPath: "/catalog",
        noIndex: false,
      },
      {
        hasCanonicalNormalization: true,
      },
    ),
    {
      pageLoaderKind: "seo-metadata",
      seoArea: "catalog-seo",
      seoNormalization: "canonical",
      indexability: "indexable",
      seoMetadataState: "single-locale",
      seoVisibilityFootprint: "indexable|single-locale",
      seoTargetFootprint: "indexable|/catalog",
      languageAlternateState: "missing",
      languageAlternateFootprint: "none",
      seoAlternateSummaryFootprint: "alternates:none",
      seoSummaryFootprint: "indexable|alternates:0[none]",
    },
  );

  assert.deepEqual(
    buildSeoSuccessDiagnostics("catalog-seo", {
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
      seoMetadataState: "localized",
      seoVisibilityFootprint: "indexable|localized",
      seoTargetFootprint: "indexable|/en-US/catalog",
      languageAlternateState: "present",
      languageAlternateFootprint: "de-DE|en-US",
      seoAlternateSummaryFootprint: "alternates:2[de-DE|en-US]",
      seoSummaryFootprint: "indexable|alternates:2[de-DE|en-US]",
    },
  );
});
test("buildSeoSuccessDiagnostics keeps raw single-locale metadata explicit", () => {
  assert.deepEqual(
    buildSeoSuccessDiagnostics("catalog-seo", {
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
      seoMetadataState: "single-locale",
      seoVisibilityFootprint: "indexable|single-locale",
      seoTargetFootprint: "indexable|/en-US/catalog",
      languageAlternateState: "missing",
      languageAlternateFootprint: "none",
      seoAlternateSummaryFootprint: "alternates:none",
      seoSummaryFootprint: "indexable|alternates:0[none]",
    },
  );
});
test("buildSeoSuccessDiagnostics keeps raw noindex metadata explicit", () => {
  assert.deepEqual(
    buildSeoSuccessDiagnostics("account-seo", {
      metadata: {
        title: "Account",
      },
      canonicalPath: "/en-US/account/sign-in",
      noIndex: true,
    }),
    {
      pageLoaderKind: "seo-metadata",
      seoArea: "account-seo",
      seoNormalization: "raw",
      indexability: "noindex",
      seoMetadataState: "private",
      seoVisibilityFootprint: "noindex|private",
      seoTargetFootprint: "noindex|/en-US/account/sign-in",
      languageAlternateState: "missing",
      languageAlternateFootprint: "none",
      seoAlternateSummaryFootprint: "alternates:none",
      seoSummaryFootprint: "noindex|alternates:0[none]",
    },
  );
});
