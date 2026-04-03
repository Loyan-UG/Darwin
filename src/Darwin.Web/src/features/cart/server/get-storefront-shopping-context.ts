import "server-only";
import { getPublicCart } from "@/features/cart/api/public-cart";
import {
  getAnonymousCartId,
  readCartDisplaySnapshots,
} from "@/features/cart/cookies";
import { extractCartLinkedProductSlugs } from "@/features/cart/storefront-shopping";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeStorefrontShoppingHealth } from "@/lib/route-health";

const getCachedStorefrontShoppingContext = createCachedObservedLoader({
  area: "storefront-shopping",
  operation: "load-shopping-context",
  thresholdMs: 250,
  getContext: (anonymousCartId: string | null) => ({
    anonymousCartState: anonymousCartId ? "present" : "missing",
  }),
  getSuccessContext: summarizeStorefrontShoppingHealth,
  load: async (anonymousCartId: string | null) => {
    const cartSnapshots = await readCartDisplaySnapshots();
    const cartResult = anonymousCartId
      ? await getPublicCart(anonymousCartId)
      : { data: null, status: "not-found" as const };

    return {
      anonymousCartId,
      cartResult,
      cartSnapshots,
      cartLinkedProductSlugs: extractCartLinkedProductSlugs(cartSnapshots),
    };
  },
});

export async function getStorefrontShoppingContext() {
  const anonymousCartId = await getAnonymousCartId();
  return getCachedStorefrontShoppingContext(anonymousCartId);
}
