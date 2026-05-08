import "server-only";
import { getStorefrontShoppingContext } from "@/features/cart/server/get-storefront-shopping-context";
import {
  createEmptyStorefrontContinuationContext,
  mergePublicStorefrontContext,
} from "@/features/storefront/public-storefront-context";
import { normalizeCultureArg } from "@/lib/route-context-normalization";
import { createSharedContextLoader } from "@/lib/shared-context-loader";

const getCachedPublicStorefrontCartContext = createSharedContextLoader({
  kind: "storefront-continuation",
  area: "public-storefront-cart",
  operation: "load-cart-context",
  normalizeArgs: normalizeCultureArg,
  getContext: (culture: string) => ({
    culture,
    contextScope: "cart-only",
  }),
  getSuccessContext: (result) => ({
    cartStatus: result.storefrontCartStatus,
    cartItemCount: result.storefrontCart?.items.length ?? 0,
    sharedContextFootprint: `cart:${result.storefrontCartStatus}:${result.storefrontCart?.items.length ?? 0}`,
  }),
  load: async () => {
    const shoppingContext = await getStorefrontShoppingContext();

    return mergePublicStorefrontContext(
      createEmptyStorefrontContinuationContext(),
      shoppingContext,
    );
  },
});

export function getPublicStorefrontCartContext(culture: string) {
  return getCachedPublicStorefrontCartContext(culture);
}
