import "server-only";
import { getPublicStorefrontCartContext } from "@/features/storefront/server/get-public-storefront-cart-context";

export async function getPublicAuthStorefrontContext(culture: string) {
  return getPublicStorefrontCartContext(culture);
}
