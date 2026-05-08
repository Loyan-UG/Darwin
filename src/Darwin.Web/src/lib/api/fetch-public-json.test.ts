import test from "node:test";
import assert from "node:assert/strict";
import { __resetApiFailureLogForTests } from "@/lib/api-diagnostics";
import {
  buildCachedPublicJsonArgs,
  buildPublicApiFailureResult,
  buildPublicApiFailureOutcome,
  buildPublicApiInvalidPayloadOutcome,
  buildPublicJsonRequestContext,
  buildPublicJsonRequestContextShape,
  buildPublicJsonResponseContext,
  buildPublicJsonResponseContextShape,
  buildPublicJsonParseResponseContext,
  buildPublicJsonExecutionResponseContext,
  buildPublicJsonExecutionNetworkFailureResult,
  buildPublicJsonExecutionPlan,
  buildPublicJsonExecutionPlanShape,
  buildPublicJsonExecutionRequestPlan,
  executePublicJsonExecutionPlan,
  executePublicJsonFetch,
  buildPublicJsonFetcher,
  buildPublicJsonFetchRequestInit,
  buildPublicJsonExecutionSuccessResult,
  buildPublicJsonInvalidPayloadFailure,
  buildPublicJsonParsedFailure,
  buildPublicJsonResponseContextResult,
  buildPublicJsonExecutionResponseResult,
  buildPublicJsonResponseContextParsedResponse,
  buildPublicJsonParsedSuccess,
  buildPublicJsonParsedFailureResult,
  buildPublicJsonParsedResponseResult,
  hasPublicJsonParsedFailure,
  hasPublicJsonResponseFailure,
  buildPublicJsonResponseStatusFailure,
  buildPublicJsonResponseFailure,
  getPublicJsonResponseFailureStatus,
  buildPublicJsonSuccessResponse,
  parsePublicJsonPayload,
  parsePublicJsonResponseContext,
  finalizePublicJsonResponseContext,
  completePublicJsonParsedResponse,
  finalizePublicJsonFailureOutcome,
  finalizePublicJsonParsedResponse,
  logPublicApiFailureOutcome,
  parsePublicJsonResponse,
  buildPublicApiNetworkFailureOutcome,
  buildPublicApiNetworkFailureDiagnostics,
  buildPublicJsonBody,
  buildPostPublicJsonInit,
  buildPostPublicJsonInitShape,
  buildPublicJsonExecutionContext,
  buildPublicJsonExecutionContextShape,
  buildPublicJsonRequestInit,
  buildPublicJsonRequestInitShape,
  buildPublicJsonWebApiRequestPlan,
  buildPublicJsonWebApiExecutionContext,
  executePublicJsonRequest,
  buildPublicApiResponseFailureOutcome,
  buildPublicApiResponseDiagnostics,
  buildPublicApiResponseFailureDiagnostics,
  buildPublicApiSuccessResult,
  buildPublicApiSuccessOutcome,
  buildPublicJsonHeaders,
  shouldIncludePublicJsonContentType,
  getCachedPublicJsonKey,
  getPublicApiFailureMessageKey,
  getPublicApiResponseFailureStatus,
  shouldLogPublicApiFailure,
} from "@/lib/api/public-json-request";
import { toLocalizedQueryMessage } from "@/localization";

test("buildPublicJsonHeaders keeps JSON accept canonical and adds content type only for body requests", () => {
  assert.equal(shouldIncludePublicJsonContentType(), false);
  assert.equal(
    shouldIncludePublicJsonContentType({
      body: JSON.stringify({ ok: true }),
    }),
    true,
  );

  assert.deepEqual(buildPublicJsonHeaders(), {
    Accept: "application/json",
  });

  assert.deepEqual(
    buildPublicJsonHeaders({
      body: JSON.stringify({ ok: true }),
      headers: {
        "X-Test": "1",
      },
    }),
    {
      Accept: "application/json",
      "Content-Type": "application/json",
      "X-Test": "1",
    },
  );
});

test("public JSON request helpers keep GET cache wiring and POST body shaping canonical", () => {
  assert.equal(
    buildPublicJsonBody({ ok: true }),
    JSON.stringify({ ok: true }),
  );

  assert.deepEqual(
    buildPublicJsonRequestInitShape(
      {
        cache: "force-cache",
        next: {
          revalidate: 120,
          tags: ["public:cms-pages"],
        },
      },
      {
        method: "POST",
      },
      {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
    ),
    {
      method: "POST",
      cache: "force-cache",
      next: {
        revalidate: 120,
        tags: ["public:cms-pages"],
      },
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
    },
  );

  assert.deepEqual(
    buildPublicJsonRequestInit(
      {
        cache: "force-cache",
        next: {
          revalidate: 120,
          tags: ["public:cms-pages", "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48"],
        },
      },
    ),
    {
      cache: "force-cache",
      next: {
        revalidate: 120,
        tags: ["public:cms-pages", "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48"],
      },
      headers: {
        Accept: "application/json",
      },
    },
  );

  assert.deepEqual(
    buildPostPublicJsonInitShape(JSON.stringify({ ok: true })),
    {
      method: "POST",
      body: JSON.stringify({ ok: true }),
    },
  );

  assert.deepEqual(
    buildPostPublicJsonInit({ ok: true }),
    {
      method: "POST",
      body: JSON.stringify({ ok: true }),
    },
  );
});

test("public JSON execution helpers keep request context and cached args canonical", () => {
  assert.deepEqual(
    buildPublicJsonExecutionContextShape(
      "http://localhost:5000/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      {
        cache: "force-cache",
        headers: {
          Accept: "application/json",
        },
      },
    ),
    {
      requestUrl:
        "http://localhost:5000/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      normalizedPath:
        "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      requestInit: {
        cache: "force-cache",
        headers: {
          Accept: "application/json",
        },
      },
    },
  );

  assert.deepEqual(
    buildPublicJsonExecutionContext(
      {
        requestUrl:
          "http://localhost:5000/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        cacheIdentity: {
          normalizedPath:
            "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
          revalidate: 120,
          keyTag: "public:cms-pages",
          pathTag:
            "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
          tags: [
            "public:cms-pages",
            "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
          ],
        },
        fetchCacheOptions: {
          cache: "force-cache",
      next: {
        revalidate: 120,
        tags: [
              "public:cms-pages",
              "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
            ],
          },
        },
      },
      {
        headers: {
          "X-Test": "1",
        },
      },
    ),
    {
      requestUrl:
        "http://localhost:5000/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      normalizedPath:
        "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      requestInit: {
        cache: "force-cache",
        next: {
          revalidate: 120,
          tags: [
            "public:cms-pages",
            "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
          ],
        },
        headers: {
          Accept: "application/json",
          "X-Test": "1",
        },
      },
    },
  );

  assert.deepEqual(
    buildCachedPublicJsonArgs(
      "/api/v1/public/catalog/products?pageSize=12&page=1&culture=de-DE",
      "catalog-products",
    ),
    [
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
      "catalog-products",
    ],
  );

  assert.deepEqual(
    buildPublicJsonExecutionPlanShape(
      {
        key: "cms-pages",
        normalizedPath:
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      },
      "http://localhost:5000/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      {
        cache: "force-cache",
        headers: {
          Accept: "application/json",
        },
      },
    ),
    {
      requestContext: {
        key: "cms-pages",
        normalizedPath:
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      },
      requestUrl:
        "http://localhost:5000/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      requestInit: {
        cache: "force-cache",
        headers: {
          Accept: "application/json",
        },
      },
    },
  );

  assert.deepEqual(
    buildPublicJsonExecutionPlan(
      {
        requestUrl:
          "http://localhost:5000/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        normalizedPath:
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        requestInit: {
          cache: "force-cache",
          headers: {
            Accept: "application/json",
          },
        },
      },
      "cms-pages",
    ),
    {
      requestContext: {
        key: "cms-pages",
        normalizedPath:
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      },
      requestUrl:
        "http://localhost:5000/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      requestInit: {
        cache: "force-cache",
        headers: {
          Accept: "application/json",
        },
      },
    },
  );

  assert.deepEqual(
    buildPublicJsonExecutionRequestPlan(
      {
        requestUrl:
          "http://localhost:5000/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        normalizedPath:
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        requestInit: {
          cache: "force-cache",
          headers: {
            Accept: "application/json",
          },
        },
      },
      "cms-pages",
    ),
    {
      requestContext: {
        key: "cms-pages",
        normalizedPath:
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      },
      requestUrl:
        "http://localhost:5000/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      requestInit: {
        cache: "force-cache",
        headers: {
          Accept: "application/json",
        },
      },
    },
  );
});

test("public JSON WebApi execution context keeps request planning and execution wiring canonical", () => {
  assert.deepEqual(
    buildPublicJsonWebApiRequestPlan(
      "http://localhost:5000",
      "cms-pages",
      "/api/v1/public/cms/pages?pageSize=48&page=1&culture=de-DE",
    ),
    {
      requestUrl:
        "http://localhost:5000/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        revalidate: 120,
        keyTag: "public:cms-pages",
        pathTag:
          "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        tags: [
          "public:cms-pages",
          "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        ],
      },
      fetchCacheOptions: {
        cache: "force-cache",
        next: {
          revalidate: 120,
          tags: [
            "public:cms-pages",
            "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
          ],
        },
      },
    },
  );

  assert.deepEqual(
    buildPublicJsonWebApiExecutionContext(
      "http://localhost:5000",
      "cms-pages",
      "/api/v1/public/cms/pages?pageSize=48&page=1&culture=de-DE",
      {
        headers: {
          "X-Test": "1",
        },
      },
    ),
    {
      requestUrl:
        "http://localhost:5000/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      normalizedPath:
        "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      requestInit: {
        cache: "force-cache",
        next: {
          revalidate: 120,
          tags: [
            "public:cms-pages",
            "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
          ],
        },
        headers: {
          Accept: "application/json",
          "X-Test": "1",
        },
      },
    },
  );
});

test("public JSON fetcher helper keeps request-init shaping canonical", async () => {
  assert.deepEqual(
    buildPublicJsonFetchRequestInit(
      "http://localhost:5000/api/v1/public/cms/pages",
      {
        cache: "force-cache",
      },
    ),
    {
      cache: "force-cache",
    },
  );

  assert.deepEqual(
    buildPublicJsonFetchRequestInit(
      "http://localhost:5000/api/v1/public/cms/pages",
      {
        cache: "force-cache",
      },
      (requestUrl, requestInit) => ({
        ...requestInit,
        headers: {
          ...(requestInit.headers ?? {}),
          "X-Request-Url": requestUrl,
        },
      }),
    ),
    {
      cache: "force-cache",
      headers: {
        "X-Request-Url": "http://localhost:5000/api/v1/public/cms/pages",
      },
    },
  );

  const calls: Array<{ requestUrl: string; requestInit: RequestInit }> = [];
  const fetcher = buildPublicJsonFetcher(
    async (requestUrl, requestInit) => {
      calls.push({ requestUrl, requestInit });
      return new Response(JSON.stringify({ ok: true }), { status: 200 });
    },
    (requestUrl, requestInit) => ({
      ...requestInit,
      headers: {
        ...(requestInit.headers ?? {}),
        "X-Request-Url": requestUrl,
      },
    }),
  );

  const response = await fetcher("http://localhost:5000/api/v1/public/cms/pages", {
    cache: "force-cache",
  });

  assert.equal(response.status, 200);
  assert.deepEqual(calls, [
    {
      requestUrl: "http://localhost:5000/api/v1/public/cms/pages",
      requestInit: {
        cache: "force-cache",
        headers: {
          "X-Request-Url": "http://localhost:5000/api/v1/public/cms/pages",
        },
      },
    },
  ]);
});

test("getCachedPublicJsonKey keeps equivalent public paths on one canonical cache key", () => {
  assert.equal(
    getCachedPublicJsonKey(
      "/api/v1/public/catalog/products?pageSize=12&page=1&culture=de-DE",
    ),
    "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
  );
});

test("public API failure helpers keep message mapping and result shape canonical", () => {
  assert.equal(
    getPublicApiFailureMessageKey("not-found"),
    "publicApiNotFoundMessage",
  );
  assert.equal(
    getPublicApiFailureMessageKey("http-error"),
    "publicApiHttpErrorMessage",
  );
  assert.equal(
    getPublicApiFailureMessageKey("invalid-payload"),
    "publicApiInvalidPayloadMessage",
  );
  assert.equal(
    getPublicApiFailureMessageKey("network-error"),
    "publicApiNetworkErrorMessage",
  );

  assert.deepEqual(buildPublicApiFailureResult("network-error"), {
    data: null,
    status: "network-error",
    message: toLocalizedQueryMessage("publicApiNetworkErrorMessage"),
    diagnostics: undefined,
  });

  assert.deepEqual(
    buildPublicApiFailureOutcome("http-error", {
      area: "cms-pages",
      path: "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      apiKind: "public",
      surfaceFamily: "public-discovery",
      surfaceArea: "cms-pages",
      statusCode: 500,
      statusFamily: "server-error",
      failureKind: "http-error",
      retryable: true,
      attentionLevel: "high",
      suggestedAction: "inspect-public-discovery-availability",
    }),
    {
      diagnostics: {
        area: "cms-pages",
        path: "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        apiKind: "public",
        surfaceFamily: "public-discovery",
        surfaceArea: "cms-pages",
        statusCode: 500,
        statusFamily: "server-error",
        failureKind: "http-error",
        retryable: true,
        attentionLevel: "high",
        suggestedAction: "inspect-public-discovery-availability",
      },
      result: {
        data: null,
        status: "http-error",
        message: toLocalizedQueryMessage("publicApiHttpErrorMessage"),
        diagnostics: {
          area: "cms-pages",
          path: "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
          apiKind: "public",
          surfaceFamily: "public-discovery",
          surfaceArea: "cms-pages",
          statusCode: 500,
          statusFamily: "server-error",
          failureKind: "http-error",
          retryable: true,
          attentionLevel: "high",
          suggestedAction: "inspect-public-discovery-availability",
        },
      },
    },
  );
});

test("public API response helpers keep status classification and success shape canonical", () => {
  assert.equal(getPublicApiResponseFailureStatus(200), null);
  assert.equal(getPublicApiResponseFailureStatus(404), "not-found");
  assert.equal(getPublicApiResponseFailureStatus(500), "http-error");
  assert.equal(shouldLogPublicApiFailure("not-found"), false);
  assert.equal(shouldLogPublicApiFailure("http-error"), true);
  assert.equal(shouldLogPublicApiFailure("invalid-payload"), true);
  assert.equal(shouldLogPublicApiFailure("network-error"), true);

  assert.deepEqual(buildPublicApiSuccessResult({ ok: true }), {
    data: { ok: true },
    status: "ok",
    diagnostics: undefined,
  });
});

test("public API diagnostics helpers keep response and network diagnostics canonical", () => {
  const response = new Response(null, {
    status: 404,
    headers: {
      "x-request-id": "req-123",
    },
  });
  const responseContext = buildPublicJsonResponseContext(
    buildPublicJsonRequestContext(
      "cms-pages",
      "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
    ),
    response,
  );

  assert.deepEqual(
    buildPublicApiResponseDiagnostics(responseContext),
    {
      area: "cms-pages",
      path: "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      apiKind: "public",
      surfaceFamily: "public-discovery",
      surfaceArea: "cms-pages",
      statusCode: 404,
      statusFamily: "client-error",
      requestId: "req-123",
      traceparent: undefined,
    },
  );

  assert.deepEqual(
    buildPublicApiResponseFailureDiagnostics(responseContext, "not-found"),
    {
      area: "cms-pages",
      path: "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      apiKind: "public",
      surfaceFamily: "public-discovery",
      surfaceArea: "cms-pages",
      statusCode: 404,
      requestId: "req-123",
      traceparent: undefined,
      statusFamily: "client-error",
      failureKind: "not-found",
      retryable: false,
      attentionLevel: "medium",
      suggestedAction: "inspect-public-discovery-availability",
    },
  );

  assert.deepEqual(
    buildPublicApiNetworkFailureDiagnostics(
      buildPublicJsonRequestContext(
        "catalog-products",
        "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
      ),
    ),
    {
      area: "catalog-products",
      path: "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
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

test("public API outcome helpers keep success and degraded result wiring canonical", () => {
  const response = new Response(JSON.stringify({ ok: true }), {
    status: 200,
    headers: {
      "x-request-id": "req-200",
    },
  });

  assert.deepEqual(
    buildPublicApiSuccessOutcome(
      { ok: true },
      buildPublicJsonResponseContext(
        buildPublicJsonRequestContext(
          "catalog-products",
          "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
        ),
        response,
      ),
    ),
    {
      diagnostics: {
        area: "catalog-products",
        path: "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
        apiKind: "public",
        surfaceFamily: "public-discovery",
        surfaceArea: "catalog-products",
        statusCode: 200,
        statusFamily: "success",
        requestId: "req-200",
        traceparent: undefined,
      },
      result: {
        data: { ok: true },
        status: "ok",
        diagnostics: {
          area: "catalog-products",
          path: "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
          apiKind: "public",
          surfaceFamily: "public-discovery",
          surfaceArea: "catalog-products",
          statusCode: 200,
          statusFamily: "success",
          requestId: "req-200",
          traceparent: undefined,
        },
      },
    },
  );

  assert.deepEqual(
    buildPublicApiResponseFailureOutcome(
      buildPublicJsonResponseContext(
        buildPublicJsonRequestContext(
          "cms-pages",
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        ),
        new Response(null, {
          status: 404,
        }),
      ),
      "not-found",
    ),
    {
      diagnostics: {
        area: "cms-pages",
        path: "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        apiKind: "public",
        surfaceFamily: "public-discovery",
        surfaceArea: "cms-pages",
        statusCode: 404,
        statusFamily: "client-error",
        requestId: undefined,
        traceparent: undefined,
        failureKind: "not-found",
        retryable: false,
        attentionLevel: "medium",
        suggestedAction: "inspect-public-discovery-availability",
      },
      result: {
        data: null,
        status: "not-found",
        message: toLocalizedQueryMessage("publicApiNotFoundMessage"),
        diagnostics: {
          area: "cms-pages",
          path: "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
          apiKind: "public",
          surfaceFamily: "public-discovery",
          surfaceArea: "cms-pages",
          statusCode: 404,
          statusFamily: "client-error",
          requestId: undefined,
          traceparent: undefined,
          failureKind: "not-found",
          retryable: false,
          attentionLevel: "medium",
          suggestedAction: "inspect-public-discovery-availability",
        },
      },
    },
  );

  assert.deepEqual(
    buildPublicApiInvalidPayloadOutcome(
      buildPublicJsonResponseContext(
        buildPublicJsonRequestContext(
          "catalog-products",
          "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
        ),
        new Response("oops", {
          status: 200,
        }),
      ),
    ).result.status,
    "invalid-payload",
  );

  assert.deepEqual(
    buildPublicApiNetworkFailureOutcome(
      buildPublicJsonRequestContext(
        "catalog-products",
        "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
      ),
    ).result.status,
    "network-error",
  );
});

test("public JSON response parser keeps success and invalid payload handling canonical", async () => {
  const success = await parsePublicJsonResponse<{ ok: boolean }>(
    new Response(JSON.stringify({ ok: true }), {
      status: 200,
      headers: {
        "x-request-id": "req-201",
      },
    }),
    buildPublicJsonRequestContext(
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
    ),
  );

  assert.deepEqual(success.failureStatus, undefined);
  assert.deepEqual(success.outcome.result.status, "ok");
  assert.deepEqual(success.outcome.result.data, { ok: true });

  const invalidPayload = await parsePublicJsonResponse(
    new Response("{invalid", {
      status: 200,
    }),
    buildPublicJsonRequestContext(
      "cms-pages",
      "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
    ),
  );

  assert.deepEqual(invalidPayload.failureStatus, "invalid-payload");
  assert.deepEqual(invalidPayload.outcome.result.status, "invalid-payload");
});

test("public JSON response-context parser keeps response failure and payload parsing canonical", async () => {
  const responseFailure = await parsePublicJsonResponseContext(
    buildPublicJsonResponseContext(
      buildPublicJsonRequestContext(
        "cms-pages",
        "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      ),
      new Response(null, {
        status: 404,
      }),
    ),
  );

  assert.deepEqual(responseFailure.failureStatus, "not-found");
  assert.deepEqual(responseFailure.outcome.result.status, "not-found");

  const success = await parsePublicJsonResponseContext(
    buildPublicJsonResponseContext(
      buildPublicJsonRequestContext(
        "catalog-products",
        "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
      ),
      new Response(JSON.stringify({ ok: true }), {
        status: 200,
      }),
    ),
  );

  assert.deepEqual(success.failureStatus, undefined);
  assert.deepEqual(success.outcome.result.status, "ok");
  assert.deepEqual(success.outcome.result.data, { ok: true });
});

test("public JSON response-context parsed-response helper keeps response-failure dispatch canonical", async () => {
  const responseContext = buildPublicJsonResponseContext(
    buildPublicJsonRequestContext(
      "cms-pages",
      "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
    ),
    new Response(null, {
      status: 404,
    }),
  );

  const responseFailure = buildPublicJsonResponseFailure(responseContext);
  const failure = await buildPublicJsonResponseContextParsedResponse(
    responseContext,
    responseFailure,
  );

  assert.deepEqual(failure.failureStatus, "not-found");
  assert.deepEqual(failure.outcome.result.status, "not-found");

  const successContext = buildPublicJsonResponseContext(
    buildPublicJsonRequestContext(
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
    ),
    new Response(JSON.stringify({ ok: true }), {
      status: 200,
    }),
  );

  const success = await buildPublicJsonResponseContextParsedResponse(
    successContext,
    buildPublicJsonResponseFailure(successContext),
  );

  assert.equal(success.failureStatus, undefined);
  assert.deepEqual(success.outcome.result.status, "ok");
  assert.deepEqual(success.outcome.result.data, { ok: true });
});

test("public JSON response-context result helper keeps parse and finalize canonical", async () => {
  const result = await buildPublicJsonResponseContextResult(
    buildPublicJsonResponseContext(
      buildPublicJsonRequestContext(
        "catalog-products",
        "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
      ),
      new Response(JSON.stringify({ ok: true }), {
        status: 200,
      }),
    ),
  );

  assert.deepEqual(result.status, "ok");
  assert.deepEqual(result.data, { ok: true });
});

test("public JSON execution response-result helper keeps response-context success wiring canonical", async () => {
  const result = await buildPublicJsonExecutionResponseResult(
    new Response(JSON.stringify({ ok: true }), {
      status: 200,
    }),
    buildPublicJsonRequestContext(
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
    ),
  );

  assert.equal(result.status, "ok");
  assert.deepEqual(result.data, { ok: true });
});

test("public JSON response-context finalizer keeps parse-plus-finalize dispatch canonical", async () => {
  const success = await finalizePublicJsonResponseContext(
    buildPublicJsonResponseContext(
      buildPublicJsonRequestContext(
        "catalog-products",
        "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
      ),
      new Response(JSON.stringify({ ok: true }), {
        status: 200,
      }),
    ),
  );

  assert.equal(success.status, "ok");
  assert.deepEqual(success.data, { ok: true });

  __resetApiFailureLogForTests();

  const originalError = console.error;
  const logged: unknown[][] = [];
  console.error = (...args: unknown[]) => {
    logged.push(args);
  };

  try {
    const failure = await finalizePublicJsonResponseContext(
      buildPublicJsonResponseContext(
        buildPublicJsonRequestContext(
          "cms-pages",
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        ),
        new Response(null, {
          status: 500,
        }),
      ),
    );

    assert.equal(failure.status, "http-error");
    assert.equal(logged.length, 1);
    assert.equal(logged[0]?.[0], "Darwin.Web API failure");
  } finally {
    console.error = originalError;
    __resetApiFailureLogForTests();
  }
});

test("public JSON parsed-response helpers keep canonical success and failure shape", () => {
  assert.deepEqual(
    buildPublicJsonParsedSuccess({
      result: {
        data: { ok: true },
        status: "ok",
      },
    }),
    {
      outcome: {
        result: {
          data: { ok: true },
          status: "ok",
        },
      },
    },
  );

  assert.deepEqual(
    buildPublicJsonParsedFailure(
      "http-error",
      {
        result: {
          data: null,
          status: "http-error",
        },
      },
      "http-error",
    ),
    {
      failureStatus: "http-error",
      failureDetail: "http-error",
      outcome: {
        result: {
          data: null,
          status: "http-error",
        },
      },
    },
  );
});

test("public JSON parsed-response failure detector keeps success and failure dispatch canonical", () => {
  assert.equal(
    hasPublicJsonParsedFailure({
      outcome: {
        result: {
          data: { ok: true },
          status: "ok",
        },
      },
    }),
    false,
  );

  assert.equal(
    hasPublicJsonParsedFailure({
      failureStatus: "http-error",
      failureDetail: "http-error",
      outcome: {
        result: {
          data: null,
          status: "http-error",
        },
      },
    }),
    true,
  );
});

test("public JSON parsed-response result helper keeps success and failure dispatch canonical", () => {
  assert.deepEqual(
    buildPublicJsonParsedResponseResult({
      outcome: {
        result: {
          data: { ok: true },
          status: "ok",
        },
      },
    }),
    {
      data: { ok: true },
      status: "ok",
    },
  );

  assert.deepEqual(
    buildPublicJsonParsedResponseResult({
      failureStatus: "http-error",
      failureDetail: "http-error",
      outcome: {
        result: {
          data: null,
          status: "http-error",
        },
      },
    }),
    {
      data: null,
      status: "http-error",
    },
  );
});

test("public JSON parsed-response completion keeps canonical result extraction", () => {
  assert.deepEqual(
    completePublicJsonParsedResponse({
      failureStatus: "http-error",
      failureDetail: "http-error",
      outcome: {
        result: {
          data: null,
          status: "http-error",
        },
      },
    }),
    {
      data: null,
      status: "http-error",
    },
  );
});

test("public JSON parsed-failure helper keeps failure finalization canonical", () => {
  __resetApiFailureLogForTests();

  const originalError = console.error;
  const logged: unknown[][] = [];
  console.error = (...args: unknown[]) => {
    logged.push(args);
  };

  try {
    const result = buildPublicJsonParsedFailureResult({
      failureStatus: "invalid-payload",
      failureDetail: new Error("bad-json"),
      outcome: buildPublicApiInvalidPayloadOutcome(
        buildPublicJsonResponseContext(
          buildPublicJsonRequestContext(
            "catalog-products",
            "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
          ),
          new Response("{invalid", {
            status: 200,
          }),
        ),
      ),
    });

    assert.equal(result.status, "invalid-payload");
    assert.equal(logged.length, 1);
    assert.equal(logged[0]?.[0], "Darwin.Web API failure");
  } finally {
    console.error = originalError;
    __resetApiFailureLogForTests();
  }
});

test("public JSON failure finalizer keeps logging and result completion canonical", () => {
  __resetApiFailureLogForTests();

  const originalError = console.error;
  const logged: unknown[][] = [];
  console.error = (...args: unknown[]) => {
    logged.push(args);
  };

  try {
    const result = finalizePublicJsonFailureOutcome(
      "network-error",
      buildPublicApiNetworkFailureOutcome(
        buildPublicJsonRequestContext(
          "catalog-products",
          "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
        ),
      ),
      new Error("network"),
    );

    assert.equal(result.status, "network-error");
    assert.equal(logged.length, 1);
    assert.equal(logged[0]?.[0], "Darwin.Web API failure");
  } finally {
    console.error = originalError;
    __resetApiFailureLogForTests();
  }
});

test("public JSON execution success helper keeps fetch success completion canonical", async () => {
  const result = await buildPublicJsonExecutionSuccessResult(
    new Response(JSON.stringify({ ok: true }), {
      status: 200,
      headers: {
        "x-request-id": "req-202",
      },
    }),
    buildPublicJsonRequestContext(
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
    ),
  );

  assert.equal(result.status, "ok");
  assert.deepEqual(result.data, { ok: true });
});

test("public JSON execution-plan helper keeps fetch dispatch canonical", async () => {
  const success = await executePublicJsonExecutionPlan(
    buildPublicJsonExecutionPlan(
      {
        requestUrl: "http://localhost:5000/api/v1/public/catalog/products",
        normalizedPath:
          "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
        requestInit: {
          cache: "force-cache",
        },
      },
      "catalog-products",
    ),
    async () =>
      new Response(JSON.stringify({ ok: true }), {
        status: 200,
      }),
  );

  assert.equal(success.status, "ok");
  assert.deepEqual(success.data, { ok: true });
});

test("public JSON fetch executor keeps success and network-failure dispatch canonical", async () => {
  const success = await executePublicJsonFetch(
    buildPublicJsonExecutionPlan(
      {
        requestUrl: "http://localhost:5000/api/v1/public/catalog/products",
        normalizedPath:
          "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
        requestInit: {
          cache: "force-cache",
        },
      },
      "catalog-products",
    ),
    async () =>
      new Response(JSON.stringify({ ok: true }), {
        status: 200,
      }),
  );

  assert.equal(success.status, "ok");
  assert.deepEqual(success.data, { ok: true });

  __resetApiFailureLogForTests();

  const originalError = console.error;
  const logged: unknown[][] = [];
  console.error = (...args: unknown[]) => {
    logged.push(args);
  };

  try {
    const networkFailure = await executePublicJsonFetch(
      buildPublicJsonExecutionPlan(
        {
          requestUrl: "http://localhost:5000/api/v1/public/cms/pages",
          normalizedPath:
            "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
          requestInit: {
            cache: "force-cache",
          },
        },
        "cms-pages",
      ),
      async () => {
        throw new Error("network");
      },
    );

    assert.equal(networkFailure.status, "network-error");
    assert.equal(logged.length, 1);
    assert.equal(logged[0]?.[0], "Darwin.Web API failure");
  } finally {
    console.error = originalError;
    __resetApiFailureLogForTests();
  }
});

test("public JSON response-failure helper keeps response-status degradation canonical", () => {
  assert.equal(hasPublicJsonResponseFailure(null), false);

  assert.equal(
    hasPublicJsonResponseFailure(
      buildPublicJsonResponseStatusFailure(
        buildPublicJsonResponseContext(
          buildPublicJsonRequestContext(
            "cms-pages",
            "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
          ),
          new Response(null, { status: 404 }),
        ),
        "not-found",
      ),
    ),
    true,
  );

  assert.equal(
    getPublicJsonResponseFailureStatus(
      buildPublicJsonResponseContext(
        buildPublicJsonRequestContext(
          "cms-pages",
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        ),
        new Response(null, { status: 200 }),
      ),
    ),
    null,
  );

  assert.equal(
    getPublicJsonResponseFailureStatus(
      buildPublicJsonResponseContext(
        buildPublicJsonRequestContext(
          "cms-pages",
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        ),
        new Response(null, { status: 404 }),
      ),
    ),
    "not-found",
  );

  assert.equal(
    buildPublicJsonResponseFailure(
      buildPublicJsonResponseContext(
        buildPublicJsonRequestContext(
          "cms-pages",
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        ),
        new Response(null, { status: 200 }),
      ),
    ),
    null,
  );

  const failure = buildPublicJsonResponseFailure(
    buildPublicJsonResponseContext(
      buildPublicJsonRequestContext(
        "cms-pages",
        "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      ),
      new Response(null, { status: 404 }),
    ),
  );

  assert.equal(failure?.failureStatus, "not-found");
  assert.equal(failure?.outcome.result.status, "not-found");
});

test("public JSON response-status failure helper keeps status degradation shaping canonical", () => {
  const failure = buildPublicJsonResponseStatusFailure(
    buildPublicJsonResponseContext(
      buildPublicJsonRequestContext(
        "cms-pages",
        "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      ),
      new Response(null, { status: 404 }),
    ),
    "not-found",
  );

  assert.equal(failure.failureStatus, "not-found");
  assert.equal(failure.outcome.result.status, "not-found");
});

test("public JSON invalid-payload helper keeps parse degradation canonical", () => {
  const failure = buildPublicJsonInvalidPayloadFailure(
    buildPublicJsonResponseContext(
      buildPublicJsonRequestContext(
        "catalog-products",
        "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
      ),
      new Response("oops", { status: 200 }),
    ),
    new SyntaxError("invalid json"),
  );

  assert.equal(failure.failureStatus, "invalid-payload");
  assert.equal(failure.outcome.result.status, "invalid-payload");
  assert.equal(failure.failureDetail instanceof SyntaxError, true);
});

test("public JSON success helper keeps parsed success shaping canonical", () => {
  const success = buildPublicJsonSuccessResponse(
    { ok: true },
    buildPublicJsonResponseContext(
      buildPublicJsonRequestContext(
        "catalog-products",
        "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
      ),
      new Response(JSON.stringify({ ok: true }), {
        status: 200,
        headers: {
          "x-request-id": "req-ok",
        },
      }),
    ),
  );

  assert.equal(success.failureStatus, undefined);
  assert.equal(success.outcome.result.status, "ok");
  assert.deepEqual(success.outcome.result.data, { ok: true });
});

test("public JSON payload parser keeps valid and invalid JSON shaping canonical", async () => {
  const responseContext = buildPublicJsonResponseContext(
    buildPublicJsonRequestContext(
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
    ),
    new Response(JSON.stringify({ ok: true }), {
      status: 200,
      headers: {
        "x-request-id": "req-ok",
      },
    }),
  );

  const success = await parsePublicJsonPayload<{ ok: boolean }>(responseContext);
  assert.equal(success.failureStatus, undefined);
  assert.deepEqual(success.outcome.result.data, { ok: true });

  const invalid = await parsePublicJsonPayload(
    buildPublicJsonResponseContext(
      buildPublicJsonRequestContext(
        "catalog-products",
        "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
      ),
      new Response("{invalid", { status: 200 }),
    ),
  );
  assert.equal(invalid.failureStatus, "invalid-payload");
  assert.equal(invalid.outcome.result.status, "invalid-payload");
});

test("public JSON request context keeps request-bound execution inputs canonical", () => {
  assert.deepEqual(
    buildPublicJsonRequestContextShape(
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
    ),
    {
      key: "catalog-products",
      normalizedPath:
        "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
    },
  );

  const requestContext = buildPublicJsonRequestContext(
    "catalog-products",
    "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
  );

  assert.equal(requestContext.key, "catalog-products");
  assert.equal(
    requestContext.normalizedPath,
    "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
  );
});

test("public JSON response context keeps response-bound execution inputs canonical", () => {
  const response = new Response(null, {
    status: 202,
    headers: {
      "x-request-id": "req-ctx",
    },
  });

  assert.equal(
    buildPublicJsonResponseContextShape(
      "cms-pages",
      "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      response,
    ).response.status,
    202,
  );

  const responseContext = buildPublicJsonResponseContext(
    buildPublicJsonRequestContext(
      "cms-pages",
      "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
    ),
    response,
  );

  assert.equal(responseContext.key, "cms-pages");
  assert.equal(
    responseContext.normalizedPath,
    "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
  );
  assert.equal(responseContext.response.status, 202);
  assert.equal(responseContext.response.headers.get("x-request-id"), "req-ctx");

  const parseContext = buildPublicJsonParseResponseContext(
    response,
    buildPublicJsonRequestContext(
      "cms-pages",
      "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
    ),
  );

  assert.equal(parseContext.key, "cms-pages");
  assert.equal(
    parseContext.normalizedPath,
    "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
  );
  assert.equal(parseContext.response.status, 202);

  const executionContext = buildPublicJsonExecutionResponseContext(
    response,
    buildPublicJsonRequestContext(
      "cms-pages",
      "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
    ),
  );

  assert.equal(executionContext.key, "cms-pages");
  assert.equal(
    executionContext.normalizedPath,
    "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
  );
  assert.equal(executionContext.response.status, 202);
});

test("public JSON parsed-response finalizer keeps success and failure handling canonical", () => {
  __resetApiFailureLogForTests();

  const originalError = console.error;
  const logged: unknown[][] = [];
  console.error = (...args: unknown[]) => {
    logged.push(args);
  };

  try {
    const success = finalizePublicJsonParsedResponse({
      outcome: {
        result: {
          data: { ok: true },
          status: "ok",
        },
      },
    });

    assert.deepEqual(success, {
      data: { ok: true },
      status: "ok",
    });
    assert.equal(logged.length, 0);

    const failure = finalizePublicJsonParsedResponse({
      failureStatus: "http-error",
      failureDetail: "http-error",
      outcome: buildPublicApiResponseFailureOutcome(
        buildPublicJsonResponseContext(
          buildPublicJsonRequestContext(
            "cms-pages",
            "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
          ),
          new Response(null, { status: 500 }),
        ),
        "http-error",
      ),
    });

    assert.equal(failure.status, "http-error");
    assert.equal(logged.length, 1);
    assert.equal(logged[0]?.[0], "Darwin.Web API failure");
  } finally {
    console.error = originalError;
    __resetApiFailureLogForTests();
  }
});

test("public JSON network-failure finalizer keeps degraded result and logging canonical", () => {
  __resetApiFailureLogForTests();

  const originalError = console.error;
  const logged: unknown[][] = [];
  console.error = (...args: unknown[]) => {
    logged.push(args);
  };

  try {
    const result = buildPublicJsonExecutionNetworkFailureResult(
      buildPublicJsonRequestContext(
        "catalog-products",
        "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
      ),
      new Error("network"),
    );

    assert.equal(result.status, "network-error");
    assert.equal(logged.length, 1);
    assert.equal(logged[0]?.[0], "Darwin.Web API failure");
  } finally {
    console.error = originalError;
    __resetApiFailureLogForTests();
  }
});

test("public JSON request executor keeps success and network failure handling canonical", async () => {
  const success = await executePublicJsonRequest<{ ok: boolean }>(
    {
      requestUrl: "http://localhost:5000/api/v1/public/catalog/products",
      normalizedPath:
        "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
      requestInit: {
        cache: "force-cache",
      },
    },
    "catalog-products",
    async () =>
      new Response(JSON.stringify({ ok: true }), {
        status: 200,
        headers: {
          "x-request-id": "req-202",
        },
      }),
  );

  assert.deepEqual(success.status, "ok");
  assert.deepEqual(success.data, { ok: true });

  __resetApiFailureLogForTests();

  const originalError = console.error;
  const logged: unknown[][] = [];
  console.error = (...args: unknown[]) => {
    logged.push(args);
  };

  try {
    const networkFailure = await executePublicJsonRequest(
      {
        requestUrl: "http://localhost:5000/api/v1/public/cms/pages",
        normalizedPath:
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        requestInit: {
          cache: "force-cache",
        },
      },
      "cms-pages",
      async () => {
        throw new Error("network");
      },
    );

    assert.deepEqual(networkFailure.status, "network-error");
    assert.equal(logged.length, 1);
    assert.equal(logged[0]?.[0], "Darwin.Web API failure");
  } finally {
    console.error = originalError;
    __resetApiFailureLogForTests();
  }
});

test("public API failure logging helper keeps not-found quiet and logs actionable failures once", () => {
  __resetApiFailureLogForTests();

  const originalError = console.error;
  const logged: unknown[][] = [];
  console.error = (...args: unknown[]) => {
    logged.push(args);
  };

  try {
    logPublicApiFailureOutcome(
      "not-found",
      buildPublicApiResponseFailureOutcome(
        buildPublicJsonResponseContext(
          buildPublicJsonRequestContext(
            "cms-pages",
            "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
          ),
          new Response(null, { status: 404 }),
        ),
        "not-found",
      ),
      "not-found",
    );

    logPublicApiFailureOutcome(
      "network-error",
      buildPublicApiNetworkFailureOutcome(
        buildPublicJsonRequestContext(
          "catalog-products",
          "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
        ),
      ),
      new Error("network"),
    );

    assert.equal(logged.length, 1);
    assert.equal(logged[0]?.[0], "Darwin.Web API failure");
  } finally {
    console.error = originalError;
    __resetApiFailureLogForTests();
  }
});
