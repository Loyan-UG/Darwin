import test from "node:test";
import assert from "node:assert/strict";
import {
  buildApiFailureDedupeKey,
  createDiagnostics,
  getApiKind,
  getFailureAttentionLevel,
  getFailureSuggestedAction,
  getResponseDiagnostics,
  getStatusFamily,
  getSurfaceArea,
  getSurfaceFamily,
  isRetryableFailure,
  logApiFailure,
  withFailureDiagnostics,
} from "@/lib/api-diagnostics";

test("API diagnostics helpers classify status, API kind, and operational surface directly", () => {
  assert.equal(getStatusFamily(204), "success");
  assert.equal(getStatusFamily(302), "redirect");
  assert.equal(getStatusFamily(404), "client-error");
  assert.equal(getStatusFamily(503), "server-error");

  assert.equal(getApiKind("/api/v1/public/catalog/products"), "public");
  assert.equal(getApiKind("/api/v1/member/orders"), "member");
  assert.equal(getApiKind("/api/v1/auth/password/reset"), "auth");
  assert.equal(getApiKind("/custom/path"), "other");

  assert.equal(
    getSurfaceFamily("/api/v1/public/cms/menus/main-navigation", "cms-menu"),
    "shell",
  );
  assert.equal(
    getSurfaceFamily("/api/v1/public/catalog/products", "public-api"),
    "public-discovery",
  );
  assert.equal(
    getSurfaceFamily("/api/v1/public/cart/current", "cart-api"),
    "commerce",
  );
  assert.equal(
    getSurfaceFamily("/api/v1/member/orders", "member-api"),
    "member",
  );
  assert.equal(
    getSurfaceFamily("/api/v1/auth/sign-in", "auth-api"),
    "auth",
  );

  assert.equal(
    getSurfaceArea("/api/v1/public/cms/pages", "public-api"),
    "cms-pages",
  );
  assert.equal(
    getSurfaceArea("/api/v1/public/catalog/categories", "public-api"),
    "catalog-categories",
  );
  assert.equal(
    getSurfaceArea("/api/v1/public/catalog/products", "public-api"),
    "catalog-products",
  );
  assert.equal(
    getSurfaceArea("/api/v1/public/cart/current", "cart-api"),
    "cart",
  );
  assert.equal(
    getSurfaceArea("/api/v1/member/invoices", "member-api"),
    "member-invoices",
  );
});

test("API diagnostics helpers classify retryability, attention, and suggested action directly", () => {
  assert.equal(isRetryableFailure("network-error"), true);
  assert.equal(isRetryableFailure("invalid-payload"), true);
  assert.equal(isRetryableFailure("http-error", 503), true);
  assert.equal(isRetryableFailure("http-error", 429), true);
  assert.equal(isRetryableFailure("http-error", 404), false);
  assert.equal(isRetryableFailure("unauthorized", 401), false);

  assert.equal(getFailureAttentionLevel("network-error"), "high");
  assert.equal(getFailureAttentionLevel("invalid-payload", 200), "high");
  assert.equal(getFailureAttentionLevel("http-error", 503), "high");
  assert.equal(getFailureAttentionLevel("http-error", 404), "medium");

  assert.equal(
    getFailureSuggestedAction({
      failureKind: "network-error",
      retryable: true,
      surfaceFamily: "public-discovery",
    }),
    "inspect-public-discovery-connectivity",
  );
  assert.equal(
    getFailureSuggestedAction({
      failureKind: "invalid-payload",
      retryable: true,
      surfaceFamily: "member",
    }),
    "inspect-member-contract",
  );
  assert.equal(
    getFailureSuggestedAction({
      failureKind: "unauthorized",
      retryable: false,
      surfaceFamily: "auth",
    }),
    "inspect-auth-access",
  );
  assert.equal(
    getFailureSuggestedAction({
      failureKind: "not-found",
      retryable: false,
      surfaceFamily: "commerce",
    }),
    "inspect-commerce-availability",
  );
  assert.equal(
    getFailureSuggestedAction({
      failureKind: "http-error",
      retryable: true,
      surfaceFamily: "shell",
    }),
    "retry-shell-request",
  );
  assert.equal(
    getFailureSuggestedAction({
      failureKind: "http-error",
      retryable: false,
      surfaceFamily: "other",
    }),
    "inspect-other-failure",
  );
});

test("getResponseDiagnostics extracts request ids and trace headers", () => {
  const response = new Response(null, {
    status: 502,
    headers: {
      "x-request-id": "req-123",
      traceparent: "00-abc-xyz-01",
    },
  });

  const diagnostics = getResponseDiagnostics(
    "public-api",
    "/api/v1/public/catalog/products",
    response,
  );

  assert.deepEqual(diagnostics, {
    area: "public-api",
    path: "/api/v1/public/catalog/products",
    apiKind: "public",
    surfaceFamily: "public-discovery",
    surfaceArea: "catalog-products",
    statusCode: 502,
    statusFamily: "server-error",
    requestId: "req-123",
    traceparent: "00-abc-xyz-01",
  });
});

test("createDiagnostics builds network-failure context without response metadata", () => {
  assert.deepEqual(createDiagnostics("member-api", "/api/v1/member/orders"), {
    area: "member-api",
    path: "/api/v1/member/orders",
    apiKind: "member",
    surfaceFamily: "member",
    surfaceArea: "member-orders",
    statusFamily: "network-error",
  });
});

test("withFailureDiagnostics classifies retryability for common API failures", () => {
  assert.deepEqual(
    withFailureDiagnostics(
      getResponseDiagnostics(
        "public-api",
        "/api/v1/public/catalog/products",
        new Response(null, { status: 503 }),
      ),
      "http-error",
    ),
    {
      area: "public-api",
      path: "/api/v1/public/catalog/products",
      apiKind: "public",
      surfaceFamily: "public-discovery",
      surfaceArea: "catalog-products",
      statusCode: 503,
      statusFamily: "server-error",
      failureKind: "http-error",
      retryable: true,
      attentionLevel: "high",
      suggestedAction: "retry-public-discovery-request",
      requestId: undefined,
      traceparent: undefined,
    },
  );

  assert.deepEqual(
    withFailureDiagnostics(
      getResponseDiagnostics(
        "member-api",
        "/api/v1/member/orders",
        new Response(null, { status: 403 }),
      ),
      "unauthorized",
    ),
    {
      area: "member-api",
      path: "/api/v1/member/orders",
      apiKind: "member",
      surfaceFamily: "member",
      surfaceArea: "member-orders",
      statusCode: 403,
      statusFamily: "client-error",
      failureKind: "unauthorized",
      retryable: false,
      attentionLevel: "medium",
      suggestedAction: "inspect-member-access",
      requestId: undefined,
      traceparent: undefined,
    },
  );
});

test("buildApiFailureDedupeKey keeps canonical failure identity stable", () => {
  assert.equal(
    buildApiFailureDedupeKey({
      area: "public-api",
      path: "/api/v1/public/catalog/products",
      statusCode: 503,
      failureKind: "http-error",
      requestId: "req-123",
      traceparent: "trace-abc",
    }),
    "public-api|/api/v1/public/catalog/products|503|http-error|req-123|trace-abc",
  );

  assert.equal(
    buildApiFailureDedupeKey({
      area: "public-api",
      path: "/api/v1/public/catalog/products",
    }),
    "public-api|/api/v1/public/catalog/products|no-status|no-failure-kind|no-request-id|no-traceparent",
  );
});

test("logApiFailure deduplicates repeated diagnostics before logging", () => {
  const originalError = console.error;
  const calls: unknown[][] = [];
  console.error = (...args: unknown[]) => {
    calls.push(args);
  };

  try {
    const diagnostics = withFailureDiagnostics(
      createDiagnostics("public-api", "/api/v1/public/catalog/products"),
      "network-error",
    );

    logApiFailure(diagnostics, new Error("boom"));
    logApiFailure(diagnostics, new Error("boom"));

    assert.equal(calls.length, 1);
    assert.equal(calls[0]?.[0], "Darwin.Web API failure");
  } finally {
    console.error = originalError;
  }
});
