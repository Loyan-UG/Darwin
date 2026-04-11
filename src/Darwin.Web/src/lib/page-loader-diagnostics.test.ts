import assert from "node:assert/strict";
import test from "node:test";
import {
  buildContinuationSliceFootprint,
  buildPageLoaderBaseDiagnostics,
  buildProtectedRouteFootprint,
  getPageLoaderNormalizationMode,
} from "@/lib/page-loader-diagnostics";

test("getPageLoaderNormalizationMode keeps canonical versus raw explicit", () => {
  assert.equal(getPageLoaderNormalizationMode(true), "canonical");
  assert.equal(getPageLoaderNormalizationMode(false), "raw");
  assert.equal(getPageLoaderNormalizationMode(undefined), "raw");
});

test("buildPageLoaderBaseDiagnostics keeps loader kind and normalization mode explicit", () => {
  assert.deepEqual(
    buildPageLoaderBaseDiagnostics("public-discovery", {
      hasCanonicalNormalization: true,
      extras: { route: "/catalog" },
    }),
    {
      pageLoaderKind: "public-discovery",
      pageLoaderNormalization: "canonical",
      route: "/catalog",
    },
  );

  assert.deepEqual(
    buildPageLoaderBaseDiagnostics("public-discovery", {
      extras: { route: "/cms" },
    }),
    {
      pageLoaderKind: "public-discovery",
      pageLoaderNormalization: "raw",
      route: "/cms",
    },
  );

  assert.deepEqual(buildPageLoaderBaseDiagnostics("commerce"), {
    pageLoaderKind: "commerce",
    pageLoaderNormalization: "raw",
  });

  assert.deepEqual(
    buildPageLoaderBaseDiagnostics("commerce", {
      hasCanonicalNormalization: true,
      extras: {
        route: "/checkout",
        cartState: "present",
      },
    }),
    {
      pageLoaderKind: "commerce",
      pageLoaderNormalization: "canonical",
      route: "/checkout",
      cartState: "present",
    },
  );
});

test("buildContinuationSliceFootprint keeps continuation counts compact and operational", () => {
  assert.equal(
    buildContinuationSliceFootprint({
      cmsCount: 2,
      categoryCount: 1,
      productCount: 3,
      cartState: "missing",
    }),
    "cms:2|categories:1|products:3|cart:missing",
  );

  assert.equal(
    buildContinuationSliceFootprint({
      cmsCount: 0,
      categoryCount: 0,
      productCount: 0,
      cartState: "present",
    }),
    "cms:0|categories:0|products:0|cart:present",
  );

  assert.equal(
    buildContinuationSliceFootprint({
      cmsCount: 4,
      categoryCount: 0,
      productCount: 1,
      cartState: "present",
    }),
    "cms:4|categories:0|products:1|cart:present",
  );
});

test("buildProtectedRouteFootprint keeps auth and fallback state together", () => {
  assert.equal(
    buildProtectedRouteFootprint({
      authGate: "guest-fallback",
      routeContextState: "guest-fallback",
      storefrontFallbackState: "present",
    }),
    "auth:guest-fallback|route:guest-fallback|storefront:present",
  );

  assert.equal(
    buildProtectedRouteFootprint({
      authGate: "authorized",
      routeContextState: "loaded",
      storefrontFallbackState: "missing",
    }),
    "auth:authorized|route:loaded|storefront:missing",
  );

  assert.equal(
    buildProtectedRouteFootprint({
      authGate: "authorized",
      routeContextState: "loaded",
      storefrontFallbackState: "present",
    }),
    "auth:authorized|route:loaded|storefront:present",
  );

  assert.equal(
    buildProtectedRouteFootprint({
      authGate: "guest-fallback",
      routeContextState: "guest-fallback",
      storefrontFallbackState: "missing",
    }),
    "auth:guest-fallback|route:guest-fallback|storefront:missing",
  );
});

test("buildPageLoaderBaseDiagnostics keeps raw member-protected diagnostics explicit", () => {
  assert.deepEqual(
    buildPageLoaderBaseDiagnostics("member-protected", {
      extras: {
        entryRoute: "/orders/order-1",
        authGate: "authorized",
      },
    }),
    {
      pageLoaderKind: "member-protected",
      pageLoaderNormalization: "raw",
      entryRoute: "/orders/order-1",
      authGate: "authorized",
    },
  );

  assert.deepEqual(
    buildPageLoaderBaseDiagnostics("member-protected", {
      hasCanonicalNormalization: true,
      extras: {
        entryRoute: "/account",
        authGate: "guest-fallback",
      },
    }),
    {
      pageLoaderKind: "member-protected",
      pageLoaderNormalization: "canonical",
      entryRoute: "/account",
      authGate: "guest-fallback",
    },
  );
});

test("buildPageLoaderBaseDiagnostics keeps bare loader defaults explicit", () => {
  assert.deepEqual(buildPageLoaderBaseDiagnostics("public-discovery"), {
    pageLoaderKind: "public-discovery",
    pageLoaderNormalization: "raw",
  });

  assert.deepEqual(buildPageLoaderBaseDiagnostics("commerce"), {
    pageLoaderKind: "commerce",
    pageLoaderNormalization: "raw",
  });

  assert.deepEqual(buildPageLoaderBaseDiagnostics("member-protected"), {
    pageLoaderKind: "member-protected",
    pageLoaderNormalization: "raw",
  });
});


