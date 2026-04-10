import {
  createDiagnostics,
  getResponseDiagnostics,
  logApiFailure,
  withFailureDiagnostics,
  type ApiDiagnostics,
} from "@/lib/api-diagnostics";
import type {
  PublicApiFetchCacheOptions,
  PublicApiRequestPlan,
} from "@/lib/public-api-cache";
import {
  getPublicApiRequestPlan,
  normalizePublicApiCachePath,
} from "@/lib/public-api-cache";
import { toLocalizedQueryMessage } from "@/localization";

export type PublicApiFailureStatus =
  | "not-found"
  | "network-error"
  | "http-error"
  | "invalid-payload";

export type PublicApiResult<T> = {
  data: T | null;
  status:
    | "ok"
    | "not-found"
    | "network-error"
    | "http-error"
    | "invalid-payload";
  message?: string;
  diagnostics?: ApiDiagnostics;
};

export type PublicApiOutcome<T> = {
  result: PublicApiResult<T>;
  diagnostics?: ApiDiagnostics;
};

export type PublicJsonExecutionContext = {
  requestUrl: string;
  normalizedPath: string;
  requestInit: RequestInit;
};

export type PublicJsonExecutionPlan = {
  requestContext: PublicJsonRequestContext;
  requestUrl: string;
  requestInit: RequestInit;
};

export type PublicJsonParsedResponse<T> = {
  outcome: PublicApiOutcome<T>;
  failureDetail?: unknown;
  failureStatus?: PublicApiFailureStatus;
};

export type PublicJsonFailedResponse<T> = PublicJsonParsedResponse<T> & {
  failureStatus: PublicApiFailureStatus;
};

export type PublicJsonRequestContext = {
  key: string;
  normalizedPath: string;
};

export type PublicJsonResponseContext = {
  key: string;
  normalizedPath: string;
  response: Response;
};

export type PublicJsonFetcher = (
  requestUrl: string,
  requestInit: RequestInit,
) => Promise<Response>;

export type PublicJsonFetchInitBuilder = (
  requestUrl: string,
  requestInit: RequestInit,
) => RequestInit;

export function buildPublicJsonRequestContext(
  key: string,
  normalizedPath: string,
): PublicJsonRequestContext {
  return buildPublicJsonRequestContextShape(key, normalizedPath);
}

export function buildPublicJsonRequestContextShape(
  key: string,
  normalizedPath: string,
): PublicJsonRequestContext {
  return {
    key,
    normalizedPath,
  };
}

export function buildPublicJsonResponseContext(
  requestContext: PublicJsonRequestContext,
  response: Response,
): PublicJsonResponseContext {
  return buildPublicJsonResponseContextShape(
    requestContext.key,
    requestContext.normalizedPath,
    response,
  );
}

export function buildPublicJsonResponseContextShape(
  key: string,
  normalizedPath: string,
  response: Response,
): PublicJsonResponseContext {
  return {
    key,
    normalizedPath,
    response,
  };
}

export function buildPublicJsonParseResponseContext(
  response: Response,
  requestContext: PublicJsonRequestContext,
): PublicJsonResponseContext {
  return buildPublicJsonResponseContext(requestContext, response);
}

export function buildPublicJsonExecutionResponseContext(
  response: Response,
  requestContext: PublicJsonRequestContext,
): PublicJsonResponseContext {
  return buildPublicJsonResponseContext(requestContext, response);
}

export function buildPublicJsonExecutionPlan(
  executionContext: PublicJsonExecutionContext,
  key: string,
): PublicJsonExecutionPlan {
  return buildPublicJsonExecutionPlanShape(
    buildPublicJsonRequestContext(key, executionContext.normalizedPath),
    executionContext.requestUrl,
    executionContext.requestInit,
  );
}

export function buildPublicJsonExecutionRequestPlan(
  executionContext: PublicJsonExecutionContext,
  key: string,
): PublicJsonExecutionPlan {
  return buildPublicJsonExecutionPlan(executionContext, key);
}

export function buildPublicJsonExecutionPlanShape(
  requestContext: PublicJsonRequestContext,
  requestUrl: string,
  requestInit: RequestInit,
): PublicJsonExecutionPlan {
  return {
    requestContext,
    requestUrl,
    requestInit,
  };
}

export function buildPublicJsonFetcher(
  fetcher: PublicJsonFetcher,
  requestInitBuilder?: PublicJsonFetchInitBuilder,
): PublicJsonFetcher {
  return (requestUrl, requestInit) =>
    fetcher(
      requestUrl,
      buildPublicJsonFetchRequestInit(
        requestUrl,
        requestInit,
        requestInitBuilder,
      ),
    );
}

export function buildPublicJsonFetchRequestInit(
  requestUrl: string,
  requestInit: RequestInit,
  requestInitBuilder?: PublicJsonFetchInitBuilder,
): RequestInit {
  return requestInitBuilder
    ? requestInitBuilder(requestUrl, requestInit)
    : requestInit;
}

export async function executePublicJsonExecutionPlan<T>(
  executionPlan: PublicJsonExecutionPlan,
  fetcher: PublicJsonFetcher,
): Promise<PublicApiResult<T>> {
  return executePublicJsonFetch<T>(executionPlan, fetcher);
}

export function shouldIncludePublicJsonContentType(init?: RequestInit) {
  return Boolean(init?.body);
}

export function buildPublicJsonHeaders(init?: RequestInit) {
  return {
    Accept: "application/json",
    ...(shouldIncludePublicJsonContentType(init)
      ? { "Content-Type": "application/json" }
      : {}),
    ...(init?.headers ?? {}),
  };
}

export function buildPublicJsonRequestInit(
  fetchCacheOptions: PublicApiFetchCacheOptions,
  init?: RequestInit,
) {
  return buildPublicJsonRequestInitShape(
    fetchCacheOptions,
    init,
    buildPublicJsonHeaders(init),
  );
}

export function buildPublicJsonRequestInitShape(
  fetchCacheOptions: PublicApiFetchCacheOptions,
  init: RequestInit | undefined,
  headers: HeadersInit,
): RequestInit {
  return {
    ...(init ?? {}),
    ...fetchCacheOptions,
    headers,
  };
}

export function buildPublicJsonBody(body: unknown): string {
  return JSON.stringify(body);
}

export function buildPublicJsonExecutionContext(
  requestPlan: PublicApiRequestPlan,
  init?: RequestInit,
): PublicJsonExecutionContext {
  return buildPublicJsonExecutionContextShape(
    requestPlan.requestUrl,
    requestPlan.cacheIdentity.normalizedPath,
    buildPublicJsonRequestInit(requestPlan.fetchCacheOptions, init),
  );
}

export function buildPublicJsonExecutionContextShape(
  requestUrl: string,
  normalizedPath: string,
  requestInit: RequestInit,
): PublicJsonExecutionContext {
  return {
    requestUrl,
    normalizedPath,
    requestInit,
  };
}

export function buildPublicJsonWebApiRequestPlan(
  webApiBaseUrl: string,
  key: string,
  path: string,
  method?: string,
): PublicApiRequestPlan {
  return getPublicApiRequestPlan(webApiBaseUrl, key, path, method);
}

export function buildPublicJsonWebApiExecutionContext(
  webApiBaseUrl: string,
  key: string,
  path: string,
  init?: RequestInit,
) {
  return buildPublicJsonExecutionContext(
    buildPublicJsonWebApiRequestPlan(webApiBaseUrl, key, path, init?.method),
    init,
  );
}

export async function executePublicJsonRequest<T>(
  executionContext: PublicJsonExecutionContext,
  key: string,
  fetcher: PublicJsonFetcher,
): Promise<PublicApiResult<T>> {
  return executePublicJsonExecutionPlan(
    buildPublicJsonExecutionRequestPlan(executionContext, key),
    fetcher,
  );
}

export function buildPostPublicJsonInit(body: unknown): RequestInit {
  return buildPostPublicJsonInitShape(buildPublicJsonBody(body));
}

export function buildPostPublicJsonInitShape(body: string): RequestInit {
  return {
    method: "POST",
    body,
  };
}

export function getCachedPublicJsonKey(path: string) {
  return normalizePublicApiCachePath(path);
}

export function buildCachedPublicJsonArgs(path: string, key: string) {
  return [getCachedPublicJsonKey(path), key] as const;
}

export function getPublicApiFailureMessageKey(status: PublicApiFailureStatus) {
  switch (status) {
    case "not-found":
      return "publicApiNotFoundMessage";
    case "http-error":
      return "publicApiHttpErrorMessage";
    case "invalid-payload":
      return "publicApiInvalidPayloadMessage";
    case "network-error":
    default:
      return "publicApiNetworkErrorMessage";
  }
}

export function shouldLogPublicApiFailure(status: PublicApiFailureStatus) {
  return status !== "not-found";
}

export function logPublicApiFailureOutcome(
  status: PublicApiFailureStatus,
  outcome: PublicApiOutcome<unknown>,
  detail: unknown,
) {
  if (outcome.diagnostics && shouldLogPublicApiFailure(status)) {
    logApiFailure(outcome.diagnostics, detail);
  }
}

export function buildPublicApiFailureResult<T>(
  status: PublicApiFailureStatus,
  diagnostics?: ApiDiagnostics,
): PublicApiResult<T> {
  return {
    data: null as T | null,
    status,
    message: toLocalizedQueryMessage(getPublicApiFailureMessageKey(status)),
    diagnostics,
  };
}

export function getPublicApiResponseFailureStatus(statusCode: number) {
  if (statusCode === 404) {
    return "not-found" as const;
  }

  if (statusCode >= 400) {
    return "http-error" as const;
  }

  return null;
}

export function buildPublicApiSuccessResult<T>(
  data: T,
  diagnostics?: ApiDiagnostics,
): PublicApiResult<T> {
  return {
    data,
    status: "ok",
    diagnostics,
  };
}

export function buildPublicApiSuccessOutcome<T>(
  data: T,
  responseContext: PublicJsonResponseContext,
): PublicApiOutcome<T> {
  const diagnostics = buildPublicApiResponseDiagnostics(responseContext);

  return {
    diagnostics,
    result: buildPublicApiSuccessResult(data, diagnostics),
  };
}

export function buildPublicApiResponseFailureDiagnostics(
  responseContext: PublicJsonResponseContext,
  status: Exclude<PublicApiFailureStatus, "network-error">,
) {
  return withFailureDiagnostics(
    buildPublicApiResponseDiagnostics(responseContext),
    status,
  );
}

export function buildPublicApiResponseDiagnostics(
  responseContext: PublicJsonResponseContext,
) {
  return getResponseDiagnostics(
    responseContext.key,
    responseContext.normalizedPath,
    responseContext.response,
  );
}

export function buildPublicApiNetworkFailureDiagnostics(
  requestContext: PublicJsonRequestContext,
) {
  return withFailureDiagnostics(
    createDiagnostics(requestContext.key, requestContext.normalizedPath),
    "network-error",
  );
}

export function buildPublicApiFailureOutcome<T>(
  status: PublicApiFailureStatus,
  diagnostics: ApiDiagnostics,
): PublicApiOutcome<T> {
  return {
    diagnostics,
    result: buildPublicApiFailureResult<T>(status, diagnostics),
  };
}

export function buildPublicApiResponseFailureOutcome<T>(
  responseContext: PublicJsonResponseContext,
  status: Exclude<PublicApiFailureStatus, "network-error">,
): PublicApiOutcome<T> {
  return buildPublicApiFailureOutcome(
    status,
    buildPublicApiResponseFailureDiagnostics(responseContext, status),
  );
}

export function buildPublicApiInvalidPayloadOutcome<T>(
  responseContext: PublicJsonResponseContext,
): PublicApiOutcome<T> {
  return buildPublicApiResponseFailureOutcome<T>(responseContext, "invalid-payload");
}

export function buildPublicApiNetworkFailureOutcome<T>(
  requestContext: PublicJsonRequestContext,
): PublicApiOutcome<T> {
  return buildPublicApiFailureOutcome(
    "network-error",
    buildPublicApiNetworkFailureDiagnostics(requestContext),
  );
}

export function buildPublicJsonParsedSuccess<T>(
  outcome: PublicApiOutcome<T>,
): PublicJsonParsedResponse<T> {
  return {
    outcome,
  };
}

export function buildPublicJsonParsedFailure<T>(
  status: PublicApiFailureStatus,
  outcome: PublicApiOutcome<T>,
  failureDetail: unknown,
): PublicJsonParsedResponse<T> {
  return {
    failureStatus: status,
    failureDetail,
    outcome,
  };
}

export function buildPublicJsonResponseStatusFailure<T>(
  responseContext: PublicJsonResponseContext,
  failureStatus: Exclude<PublicApiFailureStatus, "network-error">,
): PublicJsonParsedResponse<T> {
  return buildPublicJsonParsedFailure(
    failureStatus,
    buildPublicApiResponseFailureOutcome<T>(responseContext, failureStatus),
    failureStatus,
  );
}

export function buildPublicJsonResponseFailure<T>(
  responseContext: PublicJsonResponseContext,
): PublicJsonParsedResponse<T> | null {
  const failureStatus = getPublicJsonResponseFailureStatus(responseContext);

  if (!failureStatus) {
    return null;
  }

  return buildPublicJsonResponseStatusFailure(responseContext, failureStatus);
}

export function getPublicJsonResponseFailureStatus(
  responseContext: PublicJsonResponseContext,
) {
  return getPublicApiResponseFailureStatus(responseContext.response.status);
}

export function buildPublicJsonInvalidPayloadFailure<T>(
  responseContext: PublicJsonResponseContext,
  error: unknown,
): PublicJsonParsedResponse<T> {
  return buildPublicJsonParsedFailure(
    "invalid-payload",
    buildPublicApiInvalidPayloadOutcome<T>(responseContext),
    error,
  );
}

export function buildPublicJsonSuccessResponse<T>(
  data: T,
  responseContext: PublicJsonResponseContext,
): PublicJsonParsedResponse<T> {
  return buildPublicJsonParsedSuccess(
    buildPublicApiSuccessOutcome(data, responseContext),
  );
}

export async function parsePublicJsonPayload<T>(
  responseContext: PublicJsonResponseContext,
): Promise<PublicJsonParsedResponse<T>> {
  try {
    return buildPublicJsonSuccessResponse(
      (await responseContext.response.json()) as T,
      responseContext,
    );
  } catch (error) {
    return buildPublicJsonInvalidPayloadFailure<T>(responseContext, error);
  }
}

export async function buildPublicJsonResponseContextParsedResponse<T>(
  responseContext: PublicJsonResponseContext,
  responseFailure: PublicJsonParsedResponse<T> | null,
): Promise<PublicJsonParsedResponse<T>> {
  if (hasPublicJsonResponseFailure(responseFailure)) {
    return responseFailure;
  }

  return parsePublicJsonPayload<T>(responseContext);
}

export async function parsePublicJsonResponseContext<T>(
  responseContext: PublicJsonResponseContext,
): Promise<PublicJsonParsedResponse<T>> {
  return buildPublicJsonResponseContextParsedResponse(
    responseContext,
    buildPublicJsonResponseFailure<T>(responseContext),
  );
}

export function completePublicJsonParsedResponse<T>(
  parsedResponse: PublicJsonParsedResponse<T>,
): PublicApiResult<T> {
  return parsedResponse.outcome.result;
}

export function finalizePublicJsonFailureOutcome<T>(
  status: PublicApiFailureStatus,
  outcome: PublicApiOutcome<T>,
  detail: unknown,
): PublicApiResult<T> {
  logPublicApiFailureOutcome(status, outcome, detail);
  return outcome.result;
}

export function buildPublicJsonParsedFailureResult<T>(
  parsedResponse: PublicJsonFailedResponse<T>,
): PublicApiResult<T> {
  return finalizePublicJsonFailureOutcome(
    parsedResponse.failureStatus,
    parsedResponse.outcome,
    parsedResponse.failureDetail,
  );
}

export function hasPublicJsonParsedFailure<T>(
  parsedResponse: PublicJsonParsedResponse<T>,
): parsedResponse is PublicJsonFailedResponse<T> {
  return Boolean(parsedResponse.failureStatus);
}

export function hasPublicJsonResponseFailure<T>(
  responseFailure: PublicJsonParsedResponse<T> | null,
): responseFailure is PublicJsonFailedResponse<T> {
  return Boolean(responseFailure);
}

export function finalizePublicJsonParsedResponse<T>(
  parsedResponse: PublicJsonParsedResponse<T>,
): PublicApiResult<T> {
  return buildPublicJsonParsedResponseResult(parsedResponse);
}

export function buildPublicJsonParsedResponseResult<T>(
  parsedResponse: PublicJsonParsedResponse<T>,
): PublicApiResult<T> {
  if (hasPublicJsonParsedFailure(parsedResponse)) {
    return buildPublicJsonParsedFailureResult(parsedResponse);
  }

  return completePublicJsonParsedResponse(parsedResponse);
}

export async function finalizePublicJsonResponseContext<T>(
  responseContext: PublicJsonResponseContext,
): Promise<PublicApiResult<T>> {
  return finalizePublicJsonParsedResponse(
    await parsePublicJsonResponseContext<T>(responseContext),
  );
}

export async function buildPublicJsonResponseContextResult<T>(
  responseContext: PublicJsonResponseContext,
): Promise<PublicApiResult<T>> {
  return finalizePublicJsonResponseContext(responseContext);
}

export async function buildPublicJsonExecutionResponseResult<T>(
  response: Response,
  requestContext: PublicJsonRequestContext,
): Promise<PublicApiResult<T>> {
  return buildPublicJsonResponseContextResult(
    buildPublicJsonExecutionResponseContext(response, requestContext),
  );
}

export function buildPublicJsonExecutionNetworkFailureResult<T>(
  requestContext: PublicJsonRequestContext,
  error: unknown,
): PublicApiResult<T> {
  const outcome = buildPublicApiNetworkFailureOutcome<T>(requestContext);
  return finalizePublicJsonFailureOutcome("network-error", outcome, error);
}

export async function buildPublicJsonExecutionSuccessResult<T>(
  response: Response,
  requestContext: PublicJsonRequestContext,
): Promise<PublicApiResult<T>> {
  return buildPublicJsonExecutionResponseResult(response, requestContext);
}

export async function executePublicJsonFetch<T>(
  executionPlan: PublicJsonExecutionPlan,
  fetcher: PublicJsonFetcher,
): Promise<PublicApiResult<T>> {
  const { requestContext, requestUrl, requestInit } = executionPlan;

  try {
    const response = await fetcher(requestUrl, requestInit);
    return buildPublicJsonExecutionSuccessResult<T>(response, requestContext);
  } catch (error) {
    return buildPublicJsonExecutionNetworkFailureResult<T>(requestContext, error);
  }
}

export async function parsePublicJsonResponse<T>(
  response: Response,
  requestContext: PublicJsonRequestContext,
): Promise<PublicJsonParsedResponse<T>> {
  return parsePublicJsonResponseContext(
    buildPublicJsonParseResponseContext(response, requestContext),
  );
}
