import "server-only";
import {
  getCurrentMemberInvoice,
  getCurrentMemberLoyaltyBusinesses,
  getCurrentMemberOrder,
} from "@/features/member-portal/api/member-portal";
import {
  getMemberCommerceSummaryContext,
  getMemberIdentityContext,
  getMemberInvoicesPageContext,
  getMemberOrdersPageContext,
} from "@/features/member-portal/server/get-member-summary-context";
import { getPublicStorefrontCartContext } from "@/features/storefront/server/get-public-storefront-cart-context";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import {
  normalizeCultureArg,
  normalizeEntityRouteArgs,
  normalizePagedRouteArgs,
} from "@/lib/route-context-normalization";
import {
  summarizeMemberCollectionHealth,
  summarizeMemberDashboardHealth,
  summarizeMemberDetailHealth,
  summarizeMemberEditorHealth,
} from "@/lib/route-health";
import { memberRouteObservationContext } from "@/lib/route-observation-context";
import { summarizePublicStorefrontHealth } from "@/lib/route-health";

type MemberRouteStorefrontFootprintSource = {
  storefrontContext: Parameters<typeof summarizePublicStorefrontHealth>[0];
};

export function summarizeMemberRouteStorefrontFootprint(
  result: MemberRouteStorefrontFootprintSource,
) {
  const storefront = result.storefrontContext;

  return `cms:${storefront.cmsPagesStatus}:${storefront.cmsPages.length}|categories:${storefront.categoriesStatus}:${storefront.categories.length}|products:${storefront.productsStatus}:${storefront.products.length}|cart:${storefront.storefrontCartStatus}`;
}

export const getMemberDashboardRouteContext = createCachedObservedLoader({
  area: "member-route-context",
  operation: "load-dashboard-context",
  thresholdMs: 300,
  normalizeArgs: normalizeCultureArg,
  getContext: (culture: string) =>
    memberRouteObservationContext(culture, "/account"),
  getSuccessContext: (result) => ({
    ...summarizeMemberDashboardHealth(result),
    memberRouteStorefrontFootprint:
      summarizeMemberRouteStorefrontFootprint(result),
  }),
  load: async (culture: string) => {
    const [
      identityContext,
      commerceSummaryContext,
      loyaltyBusinessesResult,
      storefrontContext,
    ] = await Promise.all([
      getMemberIdentityContext(),
      getMemberCommerceSummaryContext(culture),
      getCurrentMemberLoyaltyBusinesses({ page: 1, pageSize: 3, culture }),
      getPublicStorefrontCartContext(culture),
    ]);

    return {
      identityContext,
      commerceSummaryContext,
      loyaltyBusinessesResult,
      storefrontContext,
    };
  },
});

export const getMemberEditorRouteContext = createCachedObservedLoader({
  area: "member-route-context",
  operation: "load-editor-context",
  thresholdMs: 275,
  normalizeArgs: normalizeCultureArg,
  getContext: (culture: string) => ({ culture, routeGroup: "account-editor" }),
  getSuccessContext: (result) => ({
    ...summarizeMemberEditorHealth(result),
    memberRouteStorefrontFootprint:
      summarizeMemberRouteStorefrontFootprint(result),
  }),
  load: async (culture: string) => {
    const [identityContext, storefrontContext] = await Promise.all([
      getMemberIdentityContext(),
      getPublicStorefrontCartContext(culture),
    ]);

    return {
      identityContext,
      storefrontContext,
    };
  },
});

export const getMemberOrdersRouteContext = createCachedObservedLoader({
  area: "member-route-context",
  operation: "load-orders-context",
  thresholdMs: 300,
  normalizeArgs: normalizePagedRouteArgs,
  getContext: (culture: string, page: number, pageSize: number) =>
    memberRouteObservationContext(culture, "/orders", {
      page,
      pageSize,
    }),
  getSuccessContext: (result) => ({
    ...summarizeMemberCollectionHealth(result),
    memberRouteStorefrontFootprint:
      summarizeMemberRouteStorefrontFootprint(result),
  }),
  load: async (culture: string, page: number, pageSize: number) => {
    const [ordersResult, storefrontContext] = await Promise.all([
      getMemberOrdersPageContext(culture, page, pageSize),
      getPublicStorefrontCartContext(culture),
    ]);

    return {
      ordersResult,
      storefrontContext,
    };
  },
});

export const getMemberInvoicesRouteContext = createCachedObservedLoader({
  area: "member-route-context",
  operation: "load-invoices-context",
  thresholdMs: 300,
  normalizeArgs: normalizePagedRouteArgs,
  getContext: (culture: string, page: number, pageSize: number) =>
    memberRouteObservationContext(culture, "/invoices", {
      page,
      pageSize,
    }),
  getSuccessContext: (result) => ({
    ...summarizeMemberCollectionHealth(result),
    memberRouteStorefrontFootprint:
      summarizeMemberRouteStorefrontFootprint(result),
  }),
  load: async (culture: string, page: number, pageSize: number) => {
    const [invoicesResult, storefrontContext] = await Promise.all([
      getMemberInvoicesPageContext(culture, page, pageSize),
      getPublicStorefrontCartContext(culture),
    ]);

    return {
      invoicesResult,
      storefrontContext,
    };
  },
});

export const getMemberOrderDetailRouteContext = createCachedObservedLoader({
  area: "member-route-context",
  operation: "load-order-detail-context",
  thresholdMs: 300,
  normalizeArgs: normalizeEntityRouteArgs,
  getContext: (culture: string, id: string) =>
    memberRouteObservationContext(culture, "/orders/[id]", { id }),
  getSuccessContext: (result) => ({
    ...summarizeMemberCollectionHealth(result),
    memberRouteStorefrontFootprint:
      summarizeMemberRouteStorefrontFootprint(result),
    detail: summarizeMemberDetailHealth(result.orderResult),
  }),
  load: async (culture: string, id: string) => {
    const [orderResult, storefrontContext] = await Promise.all([
      getCurrentMemberOrder(id, culture),
      getPublicStorefrontCartContext(culture),
    ]);

    return {
      orderResult,
      storefrontContext,
    };
  },
});

export const getMemberInvoiceDetailRouteContext = createCachedObservedLoader({
  area: "member-route-context",
  operation: "load-invoice-detail-context",
  thresholdMs: 300,
  normalizeArgs: normalizeEntityRouteArgs,
  getContext: (culture: string, id: string) =>
    memberRouteObservationContext(culture, "/invoices/[id]", { id }),
  getSuccessContext: (result) => ({
    ...summarizeMemberCollectionHealth(result),
    memberRouteStorefrontFootprint:
      summarizeMemberRouteStorefrontFootprint(result),
    detail: summarizeMemberDetailHealth(result.invoiceResult),
  }),
  load: async (culture: string, id: string) => {
    const [invoiceResult, storefrontContext] = await Promise.all([
      getCurrentMemberInvoice(id, culture),
      getPublicStorefrontCartContext(culture),
    ]);

    return {
      invoiceResult,
      storefrontContext,
    };
  },
});

