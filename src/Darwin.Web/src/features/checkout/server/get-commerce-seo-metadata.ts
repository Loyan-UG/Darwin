import "server-only";
import { commerceRouteObservationContext } from "@/lib/route-observation-context";
import { buildNoIndexMetadata } from "@/lib/seo";
import { createCachedObservedSeoMetadataLoader } from "@/lib/seo-loader";
import { getCommerceResource } from "@/localization";

type CommerceSeoRoute =
  | "/cart"
  | "/checkout"
  | "/checkout/orders/[orderId]/confirmation";

function getRouteTitle(culture: string, route: CommerceSeoRoute) {
  const copy = getCommerceResource(culture);

  switch (route) {
    case "/cart":
      return copy.cartMetaTitle;
    case "/checkout":
      return copy.checkoutMetaTitle;
    case "/checkout/orders/[orderId]/confirmation":
      return copy.confirmationMetaTitle;
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
  }
}

const getCachedCommerceSeoMetadata = createCachedObservedSeoMetadataLoader({
  area: "commerce-seo",
  operation: "load-route-seo-metadata",
  thresholdMs: 150,
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
    `/checkout/orders/${orderId}/confirmation`,
  );
