import "server-only";
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
};

const loggedFailures = new Set<string>();

function logFetchFailure(key: string, path: string, error: unknown) {
  if (process.env.NODE_ENV === "production") {
    return;
  }

  const dedupeKey = `${key}:${path}`;
  if (loggedFailures.has(dedupeKey)) {
    return;
  }

  loggedFailures.add(dedupeKey);
  console.error(`Darwin.Web public API fetch failed for ${path}`, error);
}

async function sendPublicJson<T>(
  path: string,
  key: string,
  init?: RequestInit,
): Promise<PublicApiFetchResult<T>> {
  const { webApiBaseUrl } = getSiteRuntimeConfig();

  try {
    const response = await fetch(`${webApiBaseUrl}${path}`, {
      ...(init ?? {}),
      ...(init?.method && init.method !== "GET"
        ? {
            cache: "no-store" as const,
          }
        : {
            next: {
              revalidate: 60,
            },
          }),
      headers: {
        Accept: "application/json",
        ...(init?.body ? { "Content-Type": "application/json" } : {}),
        ...(init?.headers ?? {}),
      },
    });

    if (response.status === 404) {
      return {
        data: null,
        status: "not-found",
        message: toLocalizedQueryMessage("publicApiNotFoundMessage"),
      };
    }

    if (!response.ok) {
      return {
        data: null,
        status: "http-error",
        message: toLocalizedQueryMessage("publicApiHttpErrorMessage"),
      };
    }

    try {
      return {
        data: (await response.json()) as T,
        status: "ok",
      };
    } catch (error) {
      logFetchFailure(key, path, error);
      return {
        data: null,
        status: "invalid-payload",
        message: toLocalizedQueryMessage("publicApiInvalidPayloadMessage"),
      };
    }
  } catch (error) {
    logFetchFailure(key, path, error);
    return {
      data: null,
      status: "network-error",
      message: toLocalizedQueryMessage("publicApiNetworkErrorMessage"),
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
