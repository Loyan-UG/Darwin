import test from "node:test";
import assert from "node:assert/strict";
import {
  __resetApiFailureLogForTests,
  buildApiFailureDedupeKey,
  buildApiResponseMetadata,
  buildApiSurfaceDiagnostics,
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
  readHeader,
  withFailureDiagnostics,
} from "@/lib/api-diagnostics";

test("API diagnostics helpers classify status, API kind, and operational surface directly", () => {
  assert.equal(getStatusFamily(undefined), undefined);
  assert.equal(getStatusFamily(204), "success");
  assert.equal(getStatusFamily(302), "redirect");
  assert.equal(getStatusFamily(404), "client-error");
  assert.equal(getStatusFamily(503), "server-error");
  assert.equal(getStatusFamily(101), "unknown");

  assert.equal(getApiKind("/api/v1/public/catalog/products"), "public");
  assert.equal(getApiKind("/api/v1/member/orders"), "member");
  assert.equal(getApiKind("/api/v1/customer/profile"), "member");
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
    getSurfaceFamily("/api/v1/customer/profile", "customer-api"),
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
    getSurfaceArea("/api/v1/public/checkout/intent", "checkout-api"),
    "checkout",
  );
  assert.equal(
    getSurfaceArea("/api/v1/member/invoices", "member-api"),
    "member-invoices",
  );
  assert.equal(
    getSurfaceArea("/api/v1/customer/profile", "customer-api"),
    "customer-api",
  );
  assert.equal(
    getSurfaceArea("/unmapped/path", "other-api"),
    "other-api",
  );
});

test("API diagnostics helpers classify retryability, attention, and suggested action directly", () => {
  assert.equal(isRetryableFailure("network-error"), true);
  assert.equal(isRetryableFailure("invalid-payload"), true);
  assert.equal(isRetryableFailure("http-error"), true);
  assert.equal(isRetryableFailure("http-error", 503), true);
  assert.equal(isRetryableFailure("http-error", 429), true);
  assert.equal(isRetryableFailure("http-error", 404), false);
  assert.equal(isRetryableFailure("unauthorized", 401), false);
  assert.equal(isRetryableFailure("not-found", 404), false);

  assert.equal(getFailureAttentionLevel("network-error"), "high");
  assert.equal(getFailureAttentionLevel("invalid-payload", 200), "high");
  assert.equal(getFailureAttentionLevel("http-error", 503), "high");
  assert.equal(getFailureAttentionLevel("http-error", 404), "medium");
  assert.equal(getFailureAttentionLevel("http-error"), "high");

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
      failureKind: "not-found",
      retryable: false,
      surfaceFamily: "public-discovery",
    }),
    "inspect-public-discovery-availability",
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

test("API diagnostics helper internals keep surface and response metadata explicit", () => {
  assert.deepEqual(
    buildApiSurfaceDiagnostics(
      "checkout-api",
      "/api/v1/public/checkout/intent",
    ),
    {
      area: "checkout-api",
      path: "/api/v1/public/checkout/intent",
      apiKind: "public",
      surfaceFamily: "commerce",
      surfaceArea: "checkout",
    },
  );

  assert.deepEqual(
    buildApiSurfaceDiagnostics(
      "other-api",
      "/unmapped/path",
    ),
    {
      area: "other-api",
      path: "/unmapped/path",
      apiKind: "other",
      surfaceFamily: "other",
      surfaceArea: "other-api",
    },
  );

  const response = new Response(null, {
    status: 503,
    headers: {
      "x-request-id": "  ",
      "request-id": " req-321 ",
      traceparent: " 00-trace-321 ",
    },
  });

  assert.deepEqual(buildApiResponseMetadata(response), {
    statusCode: 503,
    statusFamily: "server-error",
    requestId: "req-321",
    traceparent: "00-trace-321",
  });
});

test("readHeader trims values and skips blank fallbacks", () => {
  const response = new Response(null, {
    status: 200,
    headers: {
      "x-request-id": "   ",
      "request-id": " req-555 ",
      "x-correlation-id": " corr-555 ",
      traceparent: " 00-trace-555 ",
    },
  });

  assert.equal(
    readHeader(response, ["x-request-id", "request-id", "x-correlation-id"]),
    "req-555",
  );

  assert.equal(readHeader(response, ["traceparent"]), "00-trace-555");
  assert.equal(readHeader(response, ["missing-header"]), undefined);
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

  const fallbackResponse = new Response(null, {
    status: 404,
    headers: {
      "request-id": "req-456",
    },
  });

  assert.deepEqual(
    getResponseDiagnostics(
      "member-api",
      "/api/v1/member/orders",
      fallbackResponse,
    ),
    {
      area: "member-api",
      path: "/api/v1/member/orders",
      apiKind: "member",
      surfaceFamily: "member",
      surfaceArea: "member-orders",
      statusCode: 404,
      statusFamily: "client-error",
      requestId: "req-456",
      traceparent: undefined,
    },
  );

  const correlationResponse = new Response(null, {
    status: 503,
    headers: {
      "x-correlation-id": "corr-789",
    },
  });

  assert.deepEqual(
    getResponseDiagnostics(
      "auth-api",
      "/api/v1/auth/sign-in",
      correlationResponse,
    ),
    {
      area: "auth-api",
      path: "/api/v1/auth/sign-in",
      apiKind: "auth",
      surfaceFamily: "auth",
      surfaceArea: "auth",
      statusCode: 503,
      statusFamily: "server-error",
      requestId: "corr-789",
      traceparent: undefined,
    },
  );
});


test("getResponseDiagnostics falls through blank headers until x-correlation-id remains", () => {
  const response = new Response(null, {
    status: 503,
    headers: {
      "x-request-id": "   ",
      "request-id": "   ",
      "x-correlation-id": " corr-123 ",
      traceparent: " 00-trace-corr ",
    },
  });

  assert.deepEqual(
    getResponseDiagnostics(
      "auth-api",
      "/api/v1/auth/sign-in",
      response,
    ),
    {
      area: "auth-api",
      path: "/api/v1/auth/sign-in",
      apiKind: "auth",
      surfaceFamily: "auth",
      surfaceArea: "auth",
      statusCode: 503,
      statusFamily: "server-error",
      requestId: "corr-123",
      traceparent: "00-trace-corr",
    },
  );
});test("getResponseDiagnostics falls through blank request headers before using the next identifier", () => {
  const response = new Response(null, {
    status: 502,
    headers: {
      "x-request-id": "   ",
      "request-id": " req-999 ",
      traceparent: " 00-trace-999 ",
    },
  });

  assert.deepEqual(
    getResponseDiagnostics(
      "checkout-api",
      "/api/v1/public/checkout/intent",
      response,
    ),
    {
      area: "checkout-api",
      path: "/api/v1/public/checkout/intent",
      apiKind: "public",
      surfaceFamily: "commerce",
      surfaceArea: "checkout",
      statusCode: 502,
      statusFamily: "server-error",
      requestId: "req-999",
      traceparent: "00-trace-999",
    },
  );
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

  assert.deepEqual(
    withFailureDiagnostics(
      createDiagnostics("public-api", "/api/v1/public/catalog/products"),
      "network-error",
    ),
    {
      area: "public-api",
      path: "/api/v1/public/catalog/products",
      apiKind: "public",
      surfaceFamily: "public-discovery",
      surfaceArea: "catalog-products",
      statusFamily: "network-error",
      failureKind: "network-error",
      retryable: true,
      attentionLevel: "high",
      suggestedAction: "inspect-public-discovery-connectivity",
    },
  );
});

test("createDiagnostics keeps network-error defaults explicit for shell and fallback areas", () => {
  assert.deepEqual(
    createDiagnostics(
      "cms-menu",
      "/api/v1/public/cms/menus/main-navigation",
    ),
    {
      area: "cms-menu",
      path: "/api/v1/public/cms/menus/main-navigation",
      apiKind: "public",
      surfaceFamily: "shell",
      surfaceArea: "menu",
      statusFamily: "network-error",
    },
  );

  assert.deepEqual(
    createDiagnostics("other-api", "/unmapped/path"),
    {
      area: "other-api",
      path: "/unmapped/path",
      apiKind: "other",
      surfaceFamily: "other",
      surfaceArea: "other-api",
      statusFamily: "network-error",
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

test("logApiFailure reset clears dedupe state between runs", () => {
  const originalError = console.error;
  const calls: unknown[][] = [];
  console.error = (...args: unknown[]) => {
    calls.push(args);
  };

  try {
    __resetApiFailureLogForTests();

    const diagnostics = withFailureDiagnostics(
      createDiagnostics("public-api", "/api/v1/public/catalog/products"),
      "network-error",
    );

    logApiFailure(diagnostics, new Error("boom"));
    __resetApiFailureLogForTests();
    logApiFailure(diagnostics, new Error("boom-again"));

    assert.equal(calls.length, 2);
    assert.equal(calls[0]?.[0], "Darwin.Web API failure");
    assert.equal(calls[1]?.[0], "Darwin.Web API failure");
  } finally {
    console.error = originalError;
    __resetApiFailureLogForTests();
  }
});

test("logApiFailure keeps request identifiers in the dedupe key so distinct failures still log", () => {
  const originalError = console.error;
  const calls: unknown[][] = [];
  console.error = (...args: unknown[]) => {
    calls.push(args);
  };

  try {
    logApiFailure(
      withFailureDiagnostics(
        {
          ...createDiagnostics("public-api", "/api/v1/public/catalog/products"),
          requestId: "req-1",
        },
        "network-error",
      ),
      new Error("boom-1"),
    );

    logApiFailure(
      withFailureDiagnostics(
        {
          ...createDiagnostics("public-api", "/api/v1/public/catalog/products"),
          requestId: "req-2",
        },
        "network-error",
      ),
      new Error("boom-2"),
    );

    assert.equal(calls.length, 2);
  } finally {
    console.error = originalError;
    __resetApiFailureLogForTests();
  }
});






