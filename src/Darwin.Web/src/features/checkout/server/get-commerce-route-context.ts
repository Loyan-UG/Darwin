import "server-only";
import type { PublicStorefrontOrderConfirmation } from "@/features/checkout/types";
import { getPublicStorefrontOrderConfirmation } from "@/features/checkout/api/public-checkout";
import { getCartViewModel } from "@/features/cart/server/get-cart-view-model";
import { getMemberSession } from "@/features/member-session/cookies";
import {
  getMemberCommerceSummaryContext,
  getMemberIdentityContext,
} from "@/features/member-portal/server/get-member-summary-context";
import { getPublicStorefrontContext } from "@/features/storefront/server/get-public-storefront-context";
import { createObservedLoader } from "@/lib/observed-loader";
import {
  normalizeConfirmationResultArgs,
  normalizeConfirmationRouteArgs,
  normalizeCultureArg,
} from "@/lib/route-context-normalization";
import {
  summarizeCommerceRouteHealth,
  summarizeConfirmationResultHealth,
} from "@/lib/route-health";
import { commerceRouteObservationContext } from "@/lib/route-observation-context";
import type { PublicApiFetchResult } from "@/lib/api/fetch-public-json";
import { summarizePublicStorefrontHealth } from "@/lib/route-health";

type CommerceRouteStorefrontFootprintSource = {
  storefrontContext: Parameters<typeof summarizePublicStorefrontHealth>[0];
};

export function summarizeCommerceRouteStorefrontFootprint(
  result: CommerceRouteStorefrontFootprintSource,
) {
  const storefront = result.storefrontContext;

  return `cms:${storefront.cmsPagesStatus}:${storefront.cmsPages.length}|categories:${storefront.categoriesStatus}:${storefront.categories.length}|products:${storefront.productsStatus}:${storefront.products.length}|cart:${storefront.storefrontCartStatus}`;
}

const getObservedConfirmationResult = createObservedLoader({
  area: "commerce-route-context",
  operation: "load-confirmation-result",
  thresholdMs: 275,
  normalizeArgs: normalizeConfirmationResultArgs,
  getContext: (orderId: string, orderNumber?: string) => ({
    orderId,
    hasOrderNumber: Boolean(orderNumber),
  }),
  getSuccessContext: summarizeConfirmationResultHealth,
  load: async (orderId: string, orderNumber?: string) =>
    (getPublicStorefrontOrderConfirmation(
      orderId,
      orderNumber,
    ) as Promise<PublicApiFetchResult<PublicStorefrontOrderConfirmation>>),
});

const getObservedCartRouteContext = createObservedLoader({
  area: "commerce-route-context",
  operation: "load-cart-context",
  thresholdMs: 300,
  normalizeArgs: normalizeCultureArg,
  getContext: (culture: string) => commerceRouteObservationContext(culture, "/cart"),
  getSuccessContext: (result) => ({
    ...summarizeCommerceRouteHealth(result),
    commerceRouteStorefrontFootprint:
      summarizeCommerceRouteStorefrontFootprint(result),
  }),
  load: async (culture: string) => {
    const [model, memberSession, storefrontContext] = await Promise.all([
      getCartViewModel(culture),
      getMemberSession(),
      getPublicStorefrontContext(culture),
    ]);
    const identityContext = memberSession ? await getMemberIdentityContext() : null;

    return {
      model,
      memberSession,
      identityContext,
      storefrontContext,
    };
  },
});

const getObservedCheckoutRouteContext = createObservedLoader({
  area: "commerce-route-context",
  operation: "load-checkout-context",
  thresholdMs: 325,
  normalizeArgs: normalizeCultureArg,
  getContext: (culture: string) =>
    commerceRouteObservationContext(culture, "/checkout"),
  getSuccessContext: (result) => ({
    ...summarizeCommerceRouteHealth(result),
    commerceRouteStorefrontFootprint:
      summarizeCommerceRouteStorefrontFootprint(result),
  }),
  load: async (culture: string) => {
    const [model, memberSession, storefrontContext] = await Promise.all([
      getCartViewModel(culture),
      getMemberSession(),
      getPublicStorefrontContext(culture),
    ]);
    const [identityContext, commerceSummaryContext] = memberSession
      ? await Promise.all([
          getMemberIdentityContext(),
          getMemberCommerceSummaryContext(culture),
        ])
      : [null, null];

    return {
      model,
      memberSession,
      identityContext,
      commerceSummaryContext,
      storefrontContext,
    };
  },
});

const getObservedConfirmationRouteContext = createObservedLoader({
  area: "commerce-route-context",
  operation: "load-confirmation-context",
  thresholdMs: 325,
  normalizeArgs: normalizeConfirmationRouteArgs,
  getContext: (culture: string, orderId: string, orderNumber?: string) =>
    commerceRouteObservationContext(
      culture,
      "/checkout/orders/[orderId]/confirmation",
      {
        orderId,
        hasOrderNumber: Boolean(orderNumber),
      },
    ),
  getSuccessContext: (result) => ({
    ...summarizeCommerceRouteHealth(result),
    commerceRouteStorefrontFootprint:
      summarizeCommerceRouteStorefrontFootprint(result),
  }),
  load: async (culture: string, orderId: string, orderNumber?: string) => {
    const [confirmationResult, memberSession, storefrontContext] =
      await Promise.all([
        getObservedConfirmationResult(orderId, orderNumber),
        getMemberSession(),
        getPublicStorefrontContext(culture),
      ]);
    const commerceSummaryContext = memberSession
      ? await getMemberCommerceSummaryContext(culture)
      : null;

    return {
      confirmationResult,
      memberSession,
      commerceSummaryContext,
      storefrontContext,
    };
  },
});

export async function getCartRouteContext(culture: string) {
  return getObservedCartRouteContext(culture);
}

export async function getCheckoutRouteContext(culture: string) {
  return getObservedCheckoutRouteContext(culture);
}

export async function getConfirmationRouteContext(
  culture: string,
  orderId: string,
  orderNumber?: string,
) {
  return getObservedConfirmationRouteContext(culture, orderId, orderNumber);
}
