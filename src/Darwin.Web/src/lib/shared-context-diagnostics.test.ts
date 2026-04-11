import assert from "node:assert/strict";
import test from "node:test";
import {
  buildMemberEntryFootprint,
  buildMemberSummaryFootprint,
  buildPublicStorefrontFootprint,
  buildSharedContextBaseDiagnostics,
  buildStorefrontContinuationFootprint,
  getSharedContextNormalizationMode,
} from "@/lib/shared-context-diagnostics";

test("getSharedContextNormalizationMode keeps canonical versus raw explicit", () => {
  assert.equal(getSharedContextNormalizationMode(true), "canonical");
  assert.equal(getSharedContextNormalizationMode(false), "raw");
  assert.equal(getSharedContextNormalizationMode(), "raw");
});

test("buildSharedContextBaseDiagnostics keeps shared context kind and normalization mode explicit", () => {
  assert.deepEqual(
    buildSharedContextBaseDiagnostics("public-storefront", {
      hasCanonicalNormalization: true,
      extras: { culture: "de-DE" },
    }),
    {
      sharedContextKind: "public-storefront",
      sharedContextNormalization: "canonical",
      culture: "de-DE",
    },
  );

  assert.deepEqual(
    buildSharedContextBaseDiagnostics("storefront-continuation", {
      hasCanonicalNormalization: true,
      extras: { culture: "en-US", route: "/catalog" },
    }),
    {
      sharedContextKind: "storefront-continuation",
      sharedContextNormalization: "canonical",
      culture: "en-US",
      route: "/catalog",
    },
  );

  assert.deepEqual(
    buildSharedContextBaseDiagnostics("member-summary", {
      extras: { culture: "en-US", scope: "orders-page" },
    }),
    {
      sharedContextKind: "member-summary",
      sharedContextNormalization: "raw",
      culture: "en-US",
      scope: "orders-page",
    },
  );

  assert.deepEqual(
    buildSharedContextBaseDiagnostics("member-entry", {
      extras: { culture: "de-DE", route: "/account" },
    }),
    {
      sharedContextKind: "member-entry",
      sharedContextNormalization: "raw",
      culture: "de-DE",
      route: "/account",
    },
  );
});

test("shared context footprint helpers keep operational dependency states compact", () => {
  assert.equal(
    buildStorefrontContinuationFootprint({
      cmsStatus: "ok",
      categoriesStatus: "fallback",
      productsStatus: "ok",
    }),
    "cms:ok|categories:fallback|products:ok",
  );

  assert.equal(
    buildPublicStorefrontFootprint({
      cmsStatus: "ok",
      categoriesStatus: "ok",
      productsStatus: "fallback",
      cartStatus: "not-found",
    }),
    "cms:ok|categories:ok|products:fallback|cart:not-found",
  );

  assert.equal(
    buildMemberEntryFootprint({
      sessionState: "missing",
      storefrontState: "present",
    }),
    "session:missing|storefront:present",
  );

  assert.equal(
    buildMemberEntryFootprint({
      sessionState: "present",
      storefrontState: "missing",
    }),
    "session:present|storefront:missing",
  );
});

test("buildMemberSummaryFootprint keeps scope and leading statuses together", () => {
  assert.equal(
    buildMemberSummaryFootprint({
      scope: "identity",
      primaryStatus: "ok",
      secondaryStatus: "ok",
      tertiaryStatus: "fallback",
    }),
    "scope:identity|primary:ok|secondary:ok|tertiary:fallback",
  );

  assert.equal(
    buildMemberSummaryFootprint({
      scope: "orders-page",
      primaryStatus: "fallback",
    }),
    "scope:orders-page|primary:fallback",
  );

  assert.equal(
    buildMemberSummaryFootprint({
      scope: "commerce-summary",
      primaryStatus: "ok",
      secondaryStatus: "degraded",
    }),
    "scope:commerce-summary|primary:ok|secondary:degraded",
  );

  assert.equal(
    buildMemberSummaryFootprint({
      scope: "invoices-page",
      primaryStatus: "warning",
      tertiaryStatus: "present",
    }),
    "scope:invoices-page|primary:warning|tertiary:present",
  );
});

test("buildSharedContextBaseDiagnostics keeps bare shared context defaults explicit", () => {
  assert.deepEqual(buildSharedContextBaseDiagnostics("public-storefront"), {
    sharedContextKind: "public-storefront",
    sharedContextNormalization: "raw",
  });

  assert.deepEqual(buildSharedContextBaseDiagnostics("storefront-continuation"), {
    sharedContextKind: "storefront-continuation",
    sharedContextNormalization: "raw",
  });

  assert.deepEqual(buildSharedContextBaseDiagnostics("member-summary"), {
    sharedContextKind: "member-summary",
    sharedContextNormalization: "raw",
  });

  assert.deepEqual(buildSharedContextBaseDiagnostics("member-entry"), {
    sharedContextKind: "member-entry",
    sharedContextNormalization: "raw",
  });
});



