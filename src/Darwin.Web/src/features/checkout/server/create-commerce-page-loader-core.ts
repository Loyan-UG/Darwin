import {
  createCachedObservedLoader,
  createObservedLoader,
} from "@/lib/observed-loader";
import { buildPageLoaderBaseDiagnostics } from "@/lib/page-loader-diagnostics";

type CreateCommercePageLoaderOptions<TArgs extends unknown[], TResult> = {
  operation: string;
  cacheResult?: boolean;
  thresholdMs?: number;
  normalizeArgs?: (...args: TArgs) => TArgs;
  getContext: (...args: TArgs) => Record<string, unknown>;
  getSuccessContext: (
    result: TResult,
    ...args: TArgs
  ) => Record<string, unknown> | undefined;
  load: (...args: TArgs) => Promise<TResult>;
};

export function buildCommercePageLoaderObservationContext(
  context?: Record<string, unknown>,
  options?: {
    hasCanonicalNormalization?: boolean;
  },
) {
  return buildPageLoaderBaseDiagnostics("commerce", {
    hasCanonicalNormalization: options?.hasCanonicalNormalization,
    extras: context,
  });
}

export function buildCommercePageLoaderSuccessContext(
  context?: Record<string, unknown>,
  options?: {
    hasCanonicalNormalization?: boolean;
  },
) {
  return buildPageLoaderBaseDiagnostics("commerce", {
    hasCanonicalNormalization: options?.hasCanonicalNormalization,
    extras: context,
  });
}

export function createCommercePageLoaderCore<TArgs extends unknown[], TResult>({
  operation,
  cacheResult = true,
  thresholdMs = 325,
  normalizeArgs,
  getContext,
  getSuccessContext,
  load,
}: CreateCommercePageLoaderOptions<TArgs, TResult>) {
  const createLoader = cacheResult
    ? createCachedObservedLoader
    : createObservedLoader;

  return createLoader({
    area: "commerce-page-context",
    operation,
    thresholdMs,
    normalizeArgs,
    getContext: (...args: TArgs) =>
      buildCommercePageLoaderObservationContext(
        getContext(...args) ?? {},
        {
          hasCanonicalNormalization: Boolean(normalizeArgs),
        },
      ),
    getSuccessContext: (result: TResult, ...args: TArgs) =>
      buildCommercePageLoaderSuccessContext(
        getSuccessContext(result, ...args) ?? {},
        {
          hasCanonicalNormalization: Boolean(normalizeArgs),
        },
      ),
    load,
  });
}
