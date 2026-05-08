import "server-only";
import { getPublicAuthStorefrontContext } from "@/features/account/server/get-public-auth-storefront-context";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { normalizePublicAuthRouteArgs } from "@/lib/route-context-normalization";
import {
  summarizePublicAuthRouteHealth,
  summarizePublicStorefrontHealth,
} from "@/lib/route-health";

type PublicAuthStorefrontFootprintSource = {
  storefrontContext: Parameters<typeof summarizePublicStorefrontHealth>[0];
};

export function summarizePublicAuthStorefrontFootprint(
  result: PublicAuthStorefrontFootprintSource,
) {
  const storefront = result.storefrontContext;

  return `cms:${storefront.cmsPagesStatus}:${storefront.cmsPages.length}|categories:${storefront.categoriesStatus}:${storefront.categories.length}|products:${storefront.productsStatus}:${storefront.products.length}|cart:${storefront.storefrontCartStatus}`;
}

const getCachedPublicAuthRouteContext = createCachedObservedLoader({
  area: "public-auth-route-context",
  operation: "load-route-context",
  thresholdMs: 250,
  normalizeArgs: normalizePublicAuthRouteArgs,
  getContext: (culture: string, route: string) => ({
    culture,
    route,
  }),
  getSuccessContext: (result) => ({
    ...summarizePublicAuthRouteHealth(result),
    publicAuthStorefrontFootprint:
      summarizePublicAuthStorefrontFootprint(result),
  }),
  load: async (culture: string) => ({
    storefrontContext: await getPublicAuthStorefrontContext(culture),
  }),
});

export function getPublicAccountRouteContext(culture: string) {
  return getCachedPublicAuthRouteContext(culture, "/account");
}

export function getPublicActivationRouteContext(culture: string) {
  return getCachedPublicAuthRouteContext(culture, "/account/activation");
}

export function getPublicPasswordRouteContext(culture: string) {
  return getCachedPublicAuthRouteContext(culture, "/account/password");
}

export function getPublicRegisterRouteContext(culture: string) {
  return getCachedPublicAuthRouteContext(culture, "/account/register");
}

export function getPublicSignInRouteContext(culture: string) {
  return getCachedPublicAuthRouteContext(culture, "/account/sign-in");
}
