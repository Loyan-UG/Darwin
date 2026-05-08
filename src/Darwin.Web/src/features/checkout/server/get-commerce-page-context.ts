import "server-only";
import { getPublicProducts } from "@/features/catalog/api/public-catalog";
import type { PublicProductSummary } from "@/features/catalog/types";
import { createCommercePageLoader } from "@/features/checkout/server/create-commerce-page-loader";
import {
  getCartRouteContext,
  getCheckoutRouteContext,
  getConfirmationRouteContext,
} from "@/features/checkout/server/get-commerce-route-context";
import {
  summarizeCartPageHealth,
  summarizeCheckoutPageHealth,
  summarizeConfirmationPageHealth,
} from "@/lib/route-health";
import {
  filterProductsByExcludedCatalogPaths,
  filterProductsByPurchasedNames,
} from "@/features/storefront/shopping-follow-up";

const getCachedCartPageContext = createCommercePageLoader({
  operation: "load-cart-page-context",
  cacheResult: false,
  thresholdMs: 325,
  getContext: (culture: string) => ({
    culture,
    route: "/cart",
  }),
  getSuccessContext: summarizeCartPageHealth,
  load: async (culture: string) => {
    const routeContext = await getCartRouteContext(culture);
    let followUpProducts: PublicProductSummary[] = [];

    if (routeContext.model.cart?.items.length) {
      const productResult = await getPublicProducts({
        page: 1,
        pageSize: 6,
        culture,
      });
      const activeHrefs = new Set(
        routeContext.model.cart.items
          .map((item) => item.display?.href)
          .filter((value): value is string => Boolean(value)),
      );

      followUpProducts = filterProductsByExcludedCatalogPaths(
        productResult.data?.items ?? [],
        activeHrefs,
      ).slice(0, 3);
    }

    return {
      routeContext,
      followUpProducts,
    };
  },
});

const getCachedCheckoutPageContext = createCommercePageLoader({
  operation: "load-checkout-page-context",
  cacheResult: false,
  thresholdMs: 325,
  getContext: (culture: string) => ({
    culture,
    route: "/checkout",
  }),
  getSuccessContext: summarizeCheckoutPageHealth,
  load: async (culture: string) => ({
    routeContext: await getCheckoutRouteContext(culture),
  }),
});

const getCachedConfirmationPageContext = createCommercePageLoader({
  operation: "load-confirmation-page-context",
  cacheResult: false,
  thresholdMs: 325,
  getContext: (culture: string, orderId: string, orderNumber?: string) => ({
    culture,
    route: "/checkout/orders/[orderId]/confirmation",
    orderId,
    hasOrderNumber: Boolean(orderNumber),
  }),
  getSuccessContext: summarizeConfirmationPageHealth,
  load: async (culture: string, orderId: string, orderNumber?: string) => {
    const routeContext = await getConfirmationRouteContext(
      culture,
      orderId,
      orderNumber,
    );
    const followUpProducts = filterProductsByPurchasedNames(
      routeContext.storefrontContext.products,
      (routeContext.confirmationResult.data?.lines ?? []).map((line) => line.name),
    );

    return {
      routeContext,
      followUpProducts,
    };
  },
});

export function getCartPageContext(culture: string) {
  return getCachedCartPageContext(culture);
}

export function getCheckoutPageContext(culture: string) {
  return getCachedCheckoutPageContext(culture);
}

export function getConfirmationPageContext(
  culture: string,
  orderId: string,
  orderNumber?: string,
) {
  return getCachedConfirmationPageContext(culture, orderId, orderNumber);
}
