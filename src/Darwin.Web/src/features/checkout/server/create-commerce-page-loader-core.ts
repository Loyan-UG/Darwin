import { createCachedObservedLoader } from "@/lib/observed-loader";
import { buildPageLoaderBaseDiagnostics } from "@/lib/page-loader-diagnostics";

type CreateCommercePageLoaderOptions<TArgs extends unknown[], TResult> = {
  operation: string;
  thresholdMs?: number;
  normalizeArgs?: (...args: TArgs) => TArgs;
  getContext: (...args: TArgs) => Record<string, unknown>;
  getSuccessContext: (
    result: TResult,
    ...args: TArgs
  ) => Record<string, unknown> | undefined;
  load: (...args: TArgs) => Promise<TResult>;
};

export function createCommercePageLoaderCore<TArgs extends unknown[], TResult>({
  operation,
  thresholdMs = 325,
  normalizeArgs,
  getContext,
  getSuccessContext,
  load,
}: CreateCommercePageLoaderOptions<TArgs, TResult>) {
  return createCachedObservedLoader({
    area: "commerce-page-context",
    operation,
    thresholdMs,
    normalizeArgs,
    getContext: (...args: TArgs) =>
      buildPageLoaderBaseDiagnostics("commerce", {
        hasCanonicalNormalization: Boolean(normalizeArgs),
        extras: getContext(...args) ?? {},
      }),
    getSuccessContext: (result: TResult, ...args: TArgs) =>
      buildPageLoaderBaseDiagnostics("commerce", {
        hasCanonicalNormalization: Boolean(normalizeArgs),
        extras: getSuccessContext(result, ...args) ?? {},
      }),
    load,
  });
}
