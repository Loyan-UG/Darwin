import test from "node:test";
import assert from "node:assert/strict";
import {
  createDiagnostics,
  getResponseDiagnostics,
  withFailureDiagnostics,
} from "@/lib/api-diagnostics";

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
      statusCode: 503,
      statusFamily: "server-error",
      failureKind: "http-error",
      retryable: true,
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
      statusCode: 403,
      statusFamily: "client-error",
      failureKind: "unauthorized",
      retryable: false,
      requestId: undefined,
      traceparent: undefined,
    },
  );
});
