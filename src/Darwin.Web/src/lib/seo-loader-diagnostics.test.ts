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
        "de-DE": "/cms/impressum",
        "en-US": "/en-US/cms/imprint",
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
        "de-DE": "/cms/impressum",
        "en-US": "/en-US/cms/imprint",
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
      "de-DE": "/cms/impressum",
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
      canonicalPath: "/cms/impressum",
      noIndex: false,
      languageAlternates: {
        "de-DE": "/cms/impressum",
        "en-US": "/en-US/cms/imprint",
      },
    }),
    "indexable|/cms/impressum",
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
});

test("buildSeoSuccessDiagnostics keeps indexability and alternate footprint aligned", () => {
  assert.deepEqual(
    buildSeoSuccessDiagnostics(
      "cms-seo",
      {
        metadata: {
          title: "Impressum",
        },
        canonicalPath: "/cms/impressum",
        noIndex: false,
        languageAlternates: {
          "en-US": "/en-US/cms/imprint",
          "de-DE": "/cms/impressum",
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
      seoTargetFootprint: "indexable|/cms/impressum",
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
});
