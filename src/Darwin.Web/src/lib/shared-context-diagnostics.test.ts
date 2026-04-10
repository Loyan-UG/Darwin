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
});
