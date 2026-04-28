import "server-only";
import { commerceRouteObservationContext } from "@/lib/route-observation-context";
import { normalizeEntityRouteArgs } from "@/lib/route-context-normalization";
import { buildNoIndexMetadata } from "@/lib/seo";
import { createCachedObservedSeoMetadataLoader } from "@/lib/seo-loader";
import { getCommerceResource } from "@/localization";

type CommerceSeoRoute =
  | "/cart"
  | "/checkout"
  | "/checkout/orders/[orderId]/confirmation"
  | "/mock-checkout";

function getRouteTitle(culture: string, route: CommerceSeoRoute) {
  const copy = getCommerceResource(culture);

  switch (route) {
    case "/cart":
      return copy.cartMetaTitle;
    case "/checkout":
      return copy.checkoutMetaTitle;
    case "/checkout/orders/[orderId]/confirmation":
      return copy.confirmationMetaTitle;
    case "/mock-checkout":
      return copy.mockCheckoutMetaTitle;
  }
}

function getRouteDescription(culture: string, route: CommerceSeoRoute) {
  const copy = getCommerceResource(culture);

  switch (route) {
    case "/cart":
      return copy.cartMetaDescription;
    case "/checkout":
      return copy.checkoutMetaDescription;
    case "/checkout/orders/[orderId]/confirmation":
      return copy.confirmationMetaDescription;
    case "/mock-checkout":
      return copy.mockCheckoutMetaDescription;
  }
}

function normalizeCommerceSeoArgs(
  culture: string,
  route: CommerceSeoRoute,
  canonicalPath: string,
): [string, CommerceSeoRoute, string] {
  const [normalizedCulture, normalizedCanonicalPath] = normalizeEntityRouteArgs(
    culture,
    canonicalPath,
  );

  return [normalizedCulture, route.trim() as CommerceSeoRoute, normalizedCanonicalPath];
}

const getCachedCommerceSeoMetadata = createCachedObservedSeoMetadataLoader({
  area: "commerce-seo",
  operation: "load-route-seo-metadata",
  thresholdMs: 150,
  normalizeArgs: normalizeCommerceSeoArgs,
  getContext: (
    culture: string,
    route: CommerceSeoRoute,
    canonicalPath: string,
  ) => ({
    ...commerceRouteObservationContext(culture, route),
    canonicalPath,
  }),
  load: async (culture: string, route: CommerceSeoRoute, canonicalPath: string) => ({
    metadata: buildNoIndexMetadata(
      culture,
      getRouteTitle(culture, route),
      getRouteDescription(culture, route),
      canonicalPath,
    ),
    canonicalPath,
    noIndex: true,
    languageAlternates: {},
  }),
});

export const getCartSeoMetadata = (culture: string) =>
  getCachedCommerceSeoMetadata(culture, "/cart", "/cart");

export const getCheckoutSeoMetadata = (culture: string) =>
  getCachedCommerceSeoMetadata(culture, "/checkout", "/checkout");

export const getConfirmationSeoMetadata = (culture: string, orderId: string) =>
  getCachedCommerceSeoMetadata(
    culture,
    "/checkout/orders/[orderId]/confirmation",
    `/checkout/orders/${encodeURIComponent(orderId)}/confirmation`,
  );

export const getMockCheckoutSeoMetadata = (culture: string) =>
  getCachedCommerceSeoMetadata(culture, "/mock-checkout", "/mock-checkout");
