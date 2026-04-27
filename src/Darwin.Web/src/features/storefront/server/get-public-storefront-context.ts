import "server-only";
import { getStorefrontShoppingContext } from "@/features/cart/server/get-storefront-shopping-context";
import { mergePublicStorefrontContext } from "@/features/storefront/public-storefront-context";
import { getStorefrontContinuationContext } from "@/features/storefront/server/get-storefront-continuation-context";
import { summarizePublicStorefrontHealth } from "@/lib/route-health";
import { normalizeCultureArg } from "@/lib/route-context-normalization";
import {
  buildPublicStorefrontFootprint,
} from "@/lib/shared-context-diagnostics";
import { createSharedContextLoader } from "@/lib/shared-context-loader";
import { publicStorefrontObservationContext } from "@/lib/route-observation-context";

export function buildPublicStorefrontSuccessContext(
  result: Parameters<typeof summarizePublicStorefrontHealth>[0],
) {
  const summary = summarizePublicStorefrontHealth(result);

  return {
    ...summary,
    sharedContextFootprint: buildPublicStorefrontFootprint({
      cmsStatus: summary.cmsStatus,
      categoriesStatus: summary.categoriesStatus,
      productsStatus: summary.productsStatus,
      cartStatus: summary.cartStatus,
    }),
  };
}

const getCachedPublicStorefrontContext = createSharedContextLoader({
  kind: "public-storefront",
  area: "public-storefront",
  operation: "load-context",
  normalizeArgs: normalizeCultureArg,
  getContext: (culture: string) => publicStorefrontObservationContext(culture),
  getSuccessContext: (result) => buildPublicStorefrontSuccessContext(result),
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
