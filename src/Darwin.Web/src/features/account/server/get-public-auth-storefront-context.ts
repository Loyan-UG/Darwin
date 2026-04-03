import "server-only";
import { getPublicStorefrontContext } from "@/features/storefront/server/get-public-storefront-context";

export async function getPublicAuthStorefrontContext(culture: string) {
  return getPublicStorefrontContext(culture);
}
