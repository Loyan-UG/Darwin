import { createStorefrontContinuationSlice } from "@/features/storefront/route-projections";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import {
  buildContinuationSliceFootprint,
  buildPageLoaderBaseDiagnostics,
} from "@/lib/page-loader-diagnostics";

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
  normalizeArgs?: (...args: TArgs) => TArgs;
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
  normalizeArgs,
  getContext,
  getSuccessContext,
  loadRouteContext,
  sliceOptions,
}: CreatePublicDiscoveryPageLoaderOptions<TArgs, TRouteContext>) {
  return createCachedObservedLoader({
    area,
    operation,
    thresholdMs,
    normalizeArgs,
    getContext: (...args: TArgs) =>
      buildPageLoaderBaseDiagnostics("public-discovery", {
        hasCanonicalNormalization: Boolean(normalizeArgs),
        extras: getContext(...args) ?? {},
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
    ) => {
      const continuationCartState = result.continuationSlice.cartSummary
        ? "present"
        : "missing";

      return buildPageLoaderBaseDiagnostics("public-discovery", {
        hasCanonicalNormalization: Boolean(normalizeArgs),
        extras: {
          continuationCmsCount: result.continuationSlice.cmsPages.length,
          continuationCategoryCount: result.continuationSlice.categories.length,
          continuationProductCount: result.continuationSlice.products.length,
          continuationCartState,
          continuationSurfaceFootprint: buildContinuationSliceFootprint({
            cmsCount: result.continuationSlice.cmsPages.length,
            categoryCount: result.continuationSlice.categories.length,
            productCount: result.continuationSlice.products.length,
            cartState: continuationCartState,
          }),
          ...(getSuccessContext(result, ...args) ?? {}),
        },
      });
    },
  });
}
