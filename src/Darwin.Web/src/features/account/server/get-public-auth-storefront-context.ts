import "server-only";
import { getPublicCart } from "@/features/cart/api/public-cart";
import { getAnonymousCartId } from "@/features/cart/cookies";
import { getStorefrontContinuationContext } from "@/features/storefront/server/get-storefront-continuation-context";

export async function getPublicAuthStorefrontContext(culture: string) {
  const anonymousCartId = await getAnonymousCartId();
  const [storefrontContext, storefrontCartResult] = await Promise.all([
    getStorefrontContinuationContext(culture),
    anonymousCartId
      ? getPublicCart(anonymousCartId)
      : Promise.resolve({ data: null, status: "not-found" as const }),
  ]);

  return {
    ...storefrontContext,
    storefrontCart: storefrontCartResult.data,
    storefrontCartStatus: storefrontCartResult.status,
  };
}
