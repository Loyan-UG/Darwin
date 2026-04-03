import { createCachedObservedLoader } from "@/lib/observed-loader";

type CreateCommercePageLoaderOptions<TArgs extends unknown[], TResult> = {
  operation: string;
  thresholdMs?: number;
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
  getContext,
  getSuccessContext,
  load,
}: CreateCommercePageLoaderOptions<TArgs, TResult>) {
  return createCachedObservedLoader({
    area: "commerce-page-context",
    operation,
    thresholdMs,
    getContext,
    getSuccessContext,
    load,
  });
}
