import "server-only";
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

export async function fetchPublicJson<T>(
  path: string,
  key: string,
): Promise<PublicApiFetchResult<T>> {
  const { webApiBaseUrl } = getSiteRuntimeConfig();

  try {
    const response = await fetch(`${webApiBaseUrl}${path}`, {
      next: {
        revalidate: 60,
      },
      headers: {
        Accept: "application/json",
      },
    });

    if (response.status === 404) {
      return {
        data: null,
        status: "not-found",
        message: "Resource not found.",
      };
    }

    if (!response.ok) {
      return {
        data: null,
        status: "http-error",
        message: `Public API returned status ${response.status}.`,
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
        message: "Public API returned invalid JSON payload.",
      };
    }
  } catch (error) {
    logFetchFailure(key, path, error);
    return {
      data: null,
      status: "network-error",
      message: "Public API could not be reached.",
    };
  }
}
