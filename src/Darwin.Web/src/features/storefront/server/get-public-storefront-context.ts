import "server-only";
import { getStorefrontShoppingContext } from "@/features/cart/server/get-storefront-shopping-context";
import { mergePublicStorefrontContext } from "@/features/storefront/public-storefront-context";
import { getStorefrontContinuationContext } from "@/features/storefront/server/get-storefront-continuation-context";
import { summarizePublicStorefrontHealth } from "@/lib/route-health";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { normalizeCultureArg } from "@/lib/route-context-normalization";
import {
  buildPublicStorefrontFootprint,
  buildSharedContextBaseDiagnostics,
} from "@/lib/shared-context-diagnostics";
import { publicStorefrontObservationContext } from "@/lib/route-observation-context";

const getCachedPublicStorefrontContext = createCachedObservedLoader({
  area: "public-storefront",
  operation: "load-context",
  thresholdMs: 250,
  normalizeArgs: normalizeCultureArg,
  getContext: (culture: string) => publicStorefrontObservationContext(culture),
  getSuccessContext: (result) => {
    const summary = summarizePublicStorefrontHealth(result);

    return buildSharedContextBaseDiagnostics("public-storefront", {
      hasCanonicalNormalization: true,
      extras: {
        ...summary,
        sharedContextFootprint: buildPublicStorefrontFootprint({
          cmsStatus: summary.cmsStatus,
          categoriesStatus: summary.categoriesStatus,
          productsStatus: summary.productsStatus,
          cartStatus: summary.cartStatus,
        }),
      },
    });
  },
  load: async (culture: string) => {
    const [continuationContext, shoppingContext] = await Promise.all([
      getStorefrontContinuationContext(culture),
      getStorefrontShoppingContext(),
    ]);

    return mergePublicStorefrontContext(continuationContext, shoppingContext);
  },
});

export async function getPublicStorefrontContext(culture: string) {
  return getCachedPublicStorefrontContext(culture);
}
