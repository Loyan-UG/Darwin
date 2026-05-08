import "server-only";
import {
  getPublicActivationRouteContext,
  getPublicPasswordRouteContext,
  getPublicRegisterRouteContext,
  getPublicSignInRouteContext,
} from "@/features/account/server/get-public-auth-route-context";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizePublicAuthRouteHealth } from "@/lib/route-health";
import { createStorefrontCartProps } from "@/features/storefront/route-projections";
import { summarizePublicAuthStorefrontFootprint } from "@/features/account/server/get-public-auth-route-context";

function createPublicAuthPageLoader(
  route: "/account/sign-in" | "/account/register" | "/account/activation" | "/account/password",
  loadRouteContext: (culture: string) => Promise<{ storefrontContext: Awaited<ReturnType<typeof getPublicSignInRouteContext>>["storefrontContext"] }>,
) {
  return createCachedObservedLoader({
    area: "public-auth-page-context",
    operation: `load-${route.split("/").at(-1)}-page-context`,
    thresholdMs: 250,
    getContext: (culture: string) => ({
      culture,
      route,
    }),
    getSuccessContext: (result) => ({
      ...summarizePublicAuthRouteHealth({ ...result, route }),
    publicAuthPageStorefrontFootprint:
      summarizePublicAuthStorefrontFootprint(result),
    }),
    load: async (culture: string) => {
      const routeContext = await loadRouteContext(culture);

      return {
        ...routeContext,
        storefrontProps: createStorefrontCartProps(
          routeContext.storefrontContext,
        ),
      };
    },
  });
}

const getCachedPublicSignInPageContext = createPublicAuthPageLoader(
  "/account/sign-in",
  getPublicSignInRouteContext,
);

const getCachedPublicRegisterPageContext = createPublicAuthPageLoader(
  "/account/register",
  getPublicRegisterRouteContext,
);

const getCachedPublicActivationPageContext = createPublicAuthPageLoader(
  "/account/activation",
  getPublicActivationRouteContext,
);

const getCachedPublicPasswordPageContext = createPublicAuthPageLoader(
  "/account/password",
  getPublicPasswordRouteContext,
);

export function getPublicSignInPageContext(culture: string) {
  return getCachedPublicSignInPageContext(culture);
}

export function getPublicRegisterPageContext(culture: string) {
  return getCachedPublicRegisterPageContext(culture);
}

export function getPublicActivationPageContext(culture: string) {
  return getCachedPublicActivationPageContext(culture);
}

export function getPublicPasswordPageContext(culture: string) {
  return getCachedPublicPasswordPageContext(culture);
}

