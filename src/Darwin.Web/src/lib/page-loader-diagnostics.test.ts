import assert from "node:assert/strict";
import test from "node:test";
import {
  buildContinuationSliceFootprint,
  buildPageLoaderBaseDiagnostics,
  buildProtectedRouteFootprint,
} from "@/lib/page-loader-diagnostics";

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

  assert.deepEqual(buildPageLoaderBaseDiagnostics("commerce"), {
    pageLoaderKind: "commerce",
    pageLoaderNormalization: "raw",
  });
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
});
