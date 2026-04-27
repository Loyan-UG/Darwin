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

export function buildPublicDiscoveryPageLoaderObservationContext(
  context?: Record<string, unknown>,
  options?: {
    hasCanonicalNormalization?: boolean;
  },
) {
  return buildPageLoaderBaseDiagnostics("public-discovery", {
    hasCanonicalNormalization: options?.hasCanonicalNormalization,
    extras: context,
  });
}

export function buildPublicDiscoveryPageLoaderSuccessContext(
  continuationSlice: ReturnType<typeof createStorefrontContinuationSlice>,
  context?: Record<string, unknown>,
  options?: {
    hasCanonicalNormalization?: boolean;
  },
) {
  const continuationCartState = continuationSlice.cartSummary
    ? "present"
    : "missing";

  return buildPageLoaderBaseDiagnostics("public-discovery", {
    hasCanonicalNormalization: options?.hasCanonicalNormalization,
    extras: {
      continuationCmsCount: continuationSlice.cmsPages.length,
      continuationCategoryCount: continuationSlice.categories.length,
      continuationProductCount: continuationSlice.products.length,
      continuationCartState,
      continuationSurfaceFootprint: buildContinuationSliceFootprint({
        cmsCount: continuationSlice.cmsPages.length,
        categoryCount: continuationSlice.categories.length,
        productCount: continuationSlice.products.length,
        cartState: continuationCartState,
      }),
      ...(context ?? {}),
    },
  });
}

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
      buildPublicDiscoveryPageLoaderObservationContext(
        getContext(...args) ?? {},
        {
          hasCanonicalNormalization: Boolean(normalizeArgs),
        },
      ),
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
    ) =>
      buildPublicDiscoveryPageLoaderSuccessContext(
        result.continuationSlice,
        getSuccessContext(result, ...args) ?? {},
        {
          hasCanonicalNormalization: Boolean(normalizeArgs),
        },
      ),
  });
}
