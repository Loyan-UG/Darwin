import "server-only";
import { cache } from "react";
import {
  buildPublicJsonFetcher,
  buildCachedPublicJsonArgs,
  buildPublicJsonWebApiExecutionContext,
  buildPostPublicJsonInit,
  executePublicJsonRequest,
  type PublicApiResult,
} from "@/lib/api/public-json-request";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import { buildWebApiFetchInit } from "@/lib/webapi-fetch";

export type PublicApiFetchStatus =
  | "ok"
  | "not-found"
  | "network-error"
  | "http-error"
  | "invalid-payload";

export type PublicApiFetchResult<T> = PublicApiResult<T>;

async function sendPublicJson<T>(
  path: string,
  key: string,
  init?: RequestInit,
): Promise<PublicApiFetchResult<T>> {
  const { webApiBaseUrl } = getSiteRuntimeConfig();
  const executionContext = buildPublicJsonWebApiExecutionContext(
    webApiBaseUrl,
    key,
    path,
    init,
  );

  return executePublicJsonRequest<T>(
    executionContext,
    key,
    buildPublicJsonFetcher(fetch, buildWebApiFetchInit),
  );
}

const getCachedPublicJson = cache((path: string, key: string) =>
  sendPublicJson<unknown>(path, key),
);

export async function fetchPublicJson<T>(
  path: string,
  key: string,
): Promise<PublicApiFetchResult<T>> {
  return getCachedPublicJson(...buildCachedPublicJsonArgs(path, key)) as Promise<
    PublicApiFetchResult<T>
  >;
}

export async function postPublicJson<T>(
  path: string,
  key: string,
  body: unknown,
): Promise<PublicApiFetchResult<T>> {
  return sendPublicJson<T>(path, key, buildPostPublicJsonInit(body));
}
