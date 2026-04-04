import "server-only";
import { cache } from "react";
import {
  createDiagnostics,
  getResponseDiagnostics,
  logApiFailure,
  type ApiDiagnostics,
  withFailureDiagnostics,
} from "@/lib/api-diagnostics";
import {
  getPublicApiCachePolicy,
  normalizePublicApiCachePath,
} from "@/lib/public-api-cache";
import { toLocalizedQueryMessage } from "@/localization";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import { buildWebApiFetchInit } from "@/lib/webapi-fetch";

export type PublicApiFetchStatus =
  | "ok"
  | "not-found"
  | "network-error"
  | "http-error"
  | "invalid-payload";

export type PublicApiFetchResult<T> = {
  data: T | null;
  status: PublicApiFetchStatus;
  message?: string;
  diagnostics?: ApiDiagnostics;
};

async function sendPublicJson<T>(
  path: string,
  key: string,
  init?: RequestInit,
): Promise<PublicApiFetchResult<T>> {
  const { webApiBaseUrl } = getSiteRuntimeConfig();
  const normalizedPath = normalizePublicApiCachePath(path);
  const cachePolicy = getPublicApiCachePolicy(key, normalizedPath);

  try {
    const requestUrl = `${webApiBaseUrl}${normalizedPath}`;
    const response = await fetch(requestUrl, buildWebApiFetchInit(requestUrl, {
      ...(init ?? {}),
      ...(init?.method && init.method !== "GET"
        ? {
            cache: "no-store" as const,
          }
        : {
            cache: "force-cache" as const,
            next: {
              revalidate: cachePolicy.revalidate,
              tags: cachePolicy.tags,
            },
          }),
      headers: {
        Accept: "application/json",
        ...(init?.body ? { "Content-Type": "application/json" } : {}),
        ...(init?.headers ?? {}),
      },
    }));

    const diagnostics = getResponseDiagnostics(key, normalizedPath, response);

    if (response.status === 404) {
      const failureDiagnostics = withFailureDiagnostics(diagnostics, "not-found");
      return {
        data: null,
        status: "not-found",
        message: toLocalizedQueryMessage("publicApiNotFoundMessage"),
        diagnostics: failureDiagnostics,
      };
    }

    if (!response.ok) {
      const failureDiagnostics = withFailureDiagnostics(diagnostics, "http-error");
      logApiFailure(failureDiagnostics, "http-error");
      return {
        data: null,
        status: "http-error",
        message: toLocalizedQueryMessage("publicApiHttpErrorMessage"),
        diagnostics: failureDiagnostics,
      };
    }

    try {
      return {
        data: (await response.json()) as T,
        status: "ok",
        diagnostics,
      };
    } catch (error) {
      const failureDiagnostics = withFailureDiagnostics(
        diagnostics,
        "invalid-payload",
      );
      logApiFailure(failureDiagnostics, error);
      return {
        data: null,
        status: "invalid-payload",
        message: toLocalizedQueryMessage("publicApiInvalidPayloadMessage"),
        diagnostics: failureDiagnostics,
      };
    }
  } catch (error) {
    const diagnostics = withFailureDiagnostics(
      createDiagnostics(key, normalizedPath),
      "network-error",
    );
    logApiFailure(diagnostics, error);
    return {
      data: null,
      status: "network-error",
      message: toLocalizedQueryMessage("publicApiNetworkErrorMessage"),
      diagnostics,
    };
  }
}

const getCachedPublicJson = cache((path: string, key: string) =>
  sendPublicJson<unknown>(path, key),
);

export async function fetchPublicJson<T>(
  path: string,
  key: string,
): Promise<PublicApiFetchResult<T>> {
  return getCachedPublicJson(
    normalizePublicApiCachePath(path),
    key,
  ) as Promise<PublicApiFetchResult<T>>;
}

export async function postPublicJson<T>(
  path: string,
  key: string,
  body: unknown,
): Promise<PublicApiFetchResult<T>> {
  return sendPublicJson<T>(path, key, {
    method: "POST",
    body: JSON.stringify(body),
  });
}
