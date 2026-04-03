import { createStorefrontContinuationSlice } from "@/features/storefront/route-projections";
import { createCachedObservedLoader } from "@/lib/observed-loader";

type SliceOptions = {
  cmsCount?: number;
  categoryCount?: number;
  productCount?: number;
};

type CreatePublicDiscoveryPageLoaderOptions<
  TArgs extends unknown[],
  TRouteContext extends { storefrontContext: Parameters<typeof createStorefrontContinuationSlice>[0] },
> = {
  area: string;
  operation: string;
  thresholdMs?: number;
  getContext: (...args: TArgs) => Record<string, unknown>;
  getSuccessContext: (
    result: TRouteContext & {
      continuationSlice: ReturnType<typeof createStorefrontContinuationSlice>;
    },
    ...args: TArgs
  ) => Record<string, unknown> | undefined;
  loadRouteContext: (...args: TArgs) => Promise<TRouteContext>;
  sliceOptions?: SliceOptions;
};

export function createPublicDiscoveryPageLoaderCore<
  TArgs extends unknown[],
  TRouteContext extends { storefrontContext: Parameters<typeof createStorefrontContinuationSlice>[0] },
>({
  area,
  operation,
  thresholdMs = 300,
  getContext,
  getSuccessContext,
  loadRouteContext,
  sliceOptions,
}: CreatePublicDiscoveryPageLoaderOptions<TArgs, TRouteContext>) {
  return createCachedObservedLoader({
    area,
    operation,
    thresholdMs,
    getContext,
    getSuccessContext,
    load: async (...args: TArgs) => {
      const routeContext = await loadRouteContext(...args);

      return {
        ...routeContext,
        continuationSlice: createStorefrontContinuationSlice(
          routeContext.storefrontContext,
          sliceOptions,
        ),
      };
    },
  });
}
