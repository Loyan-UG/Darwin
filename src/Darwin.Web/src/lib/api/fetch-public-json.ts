import "server-only";
import {
  createDiagnostics,
  getResponseDiagnostics,
  logApiFailure,
  type ApiDiagnostics,
} from "@/lib/api-diagnostics";
import { getPublicApiCachePolicy } from "@/lib/public-api-cache";
import { toLocalizedQueryMessage } from "@/localization";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

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
  const cachePolicy = getPublicApiCachePolicy(key, path);

  try {
    const response = await fetch(`${webApiBaseUrl}${path}`, {
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
    });

    const diagnostics = getResponseDiagnostics(key, path, response);

    if (response.status === 404) {
      return {
        data: null,
        status: "not-found",
        message: toLocalizedQueryMessage("publicApiNotFoundMessage"),
        diagnostics,
      };
    }

    if (!response.ok) {
      logApiFailure(diagnostics, "http-error");
      return {
        data: null,
        status: "http-error",
        message: toLocalizedQueryMessage("publicApiHttpErrorMessage"),
        diagnostics,
      };
    }

    try {
      return {
        data: (await response.json()) as T,
        status: "ok",
        diagnostics,
      };
    } catch (error) {
      logApiFailure(diagnostics, error);
      return {
        data: null,
        status: "invalid-payload",
        message: toLocalizedQueryMessage("publicApiInvalidPayloadMessage"),
        diagnostics,
      };
    }
  } catch (error) {
    const diagnostics = createDiagnostics(key, path);
    logApiFailure(diagnostics, error);
    return {
      data: null,
      status: "network-error",
      message: toLocalizedQueryMessage("publicApiNetworkErrorMessage"),
      diagnostics,
    };
  }
}

export async function fetchPublicJson<T>(
  path: string,
  key: string,
): Promise<PublicApiFetchResult<T>> {
  return sendPublicJson<T>(path, key);
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
