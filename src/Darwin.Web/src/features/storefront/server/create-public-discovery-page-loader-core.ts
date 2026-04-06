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
    getContext: (...args: TArgs) => ({
      pageLoaderKind: "public-discovery",
      ...(getContext(...args) ?? {}),
    }),
    load: async (...args: TArgs) => {
      const routeContext = await loadRouteContext(...args);
      const continuationSlice = createStorefrontContinuationSlice(
        routeContext.storefrontContext,
        sliceOptions,
      );

      return {
        ...routeContext,
        continuationSlice,
      };
    },
    getSuccessContext: (
      result: TRouteContext & {
        continuationSlice: ReturnType<typeof createStorefrontContinuationSlice>;
      },
      ...args: TArgs
    ) => ({
      pageLoaderKind: "public-discovery",
      continuationCmsCount: result.continuationSlice.cmsPages.length,
      continuationCategoryCount: result.continuationSlice.categories.length,
      continuationProductCount: result.continuationSlice.products.length,
      continuationCartState: result.continuationSlice.cartSummary
        ? "present"
        : "missing",
      ...(getSuccessContext(result, ...args) ?? {}),
    }),
  });
}
