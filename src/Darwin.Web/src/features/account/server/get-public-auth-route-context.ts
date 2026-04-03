import "server-only";
import { getPublicAuthStorefrontContext } from "@/features/account/server/get-public-auth-storefront-context";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizePublicAuthRouteHealth } from "@/lib/route-health";

const getCachedPublicAuthRouteContext = createCachedObservedLoader({
  area: "public-auth-route-context",
  operation: "load-route-context",
  thresholdMs: 250,
  getContext: (culture: string, route: string) => ({
    culture,
    route,
  }),
  getSuccessContext: summarizePublicAuthRouteHealth,
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
