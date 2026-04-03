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
import { getPublicStorefrontContext } from "@/features/storefront/server/get-public-storefront-context";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import {
  summarizeMemberCollectionHealth,
  summarizeMemberDashboardHealth,
  summarizeMemberDetailHealth,
  summarizeMemberEditorHealth,
} from "@/lib/route-health";
import { memberRouteObservationContext } from "@/lib/route-observation-context";

export const getMemberDashboardRouteContext = createCachedObservedLoader({
  area: "member-route-context",
  operation: "load-dashboard-context",
  thresholdMs: 300,
  getContext: (culture: string) =>
    memberRouteObservationContext(culture, "/account"),
  getSuccessContext: summarizeMemberDashboardHealth,
  load: async (culture: string) => {
    const [
      identityContext,
      commerceSummaryContext,
      loyaltyBusinessesResult,
      storefrontContext,
    ] = await Promise.all([
      getMemberIdentityContext(),
      getMemberCommerceSummaryContext(),
      getCurrentMemberLoyaltyBusinesses({ page: 1, pageSize: 3 }),
      getPublicStorefrontContext(culture),
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
  getContext: (culture: string) => ({ culture, routeGroup: "account-editor" }),
  getSuccessContext: summarizeMemberEditorHealth,
  load: async (culture: string) => {
    const [identityContext, storefrontContext] = await Promise.all([
      getMemberIdentityContext(),
      getPublicStorefrontContext(culture),
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
  getContext: (culture: string, page: number, pageSize: number) =>
    memberRouteObservationContext(culture, "/orders", {
      page,
      pageSize,
    }),
  getSuccessContext: summarizeMemberCollectionHealth,
  load: async (culture: string, page: number, pageSize: number) => {
    const [ordersResult, storefrontContext] = await Promise.all([
      getMemberOrdersPageContext(page, pageSize),
      getPublicStorefrontContext(culture),
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
  getContext: (culture: string, page: number, pageSize: number) =>
    memberRouteObservationContext(culture, "/invoices", {
      page,
      pageSize,
    }),
  getSuccessContext: summarizeMemberCollectionHealth,
  load: async (culture: string, page: number, pageSize: number) => {
    const [invoicesResult, storefrontContext] = await Promise.all([
      getMemberInvoicesPageContext(page, pageSize),
      getPublicStorefrontContext(culture),
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
  getContext: (culture: string, id: string) =>
    memberRouteObservationContext(culture, "/orders/[id]", { id }),
  getSuccessContext: (result) => ({
    ...summarizeMemberCollectionHealth(result),
    detail: summarizeMemberDetailHealth(result.orderResult),
  }),
  load: async (culture: string, id: string) => {
    const [orderResult, storefrontContext] = await Promise.all([
      getCurrentMemberOrder(id),
      getPublicStorefrontContext(culture),
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
  getContext: (culture: string, id: string) =>
    memberRouteObservationContext(culture, "/invoices/[id]", { id }),
  getSuccessContext: (result) => ({
    ...summarizeMemberCollectionHealth(result),
    detail: summarizeMemberDetailHealth(result.invoiceResult),
  }),
  load: async (culture: string, id: string) => {
    const [invoiceResult, storefrontContext] = await Promise.all([
      getCurrentMemberInvoice(id),
      getPublicStorefrontContext(culture),
    ]);

    return {
      invoiceResult,
      storefrontContext,
    };
  },
});
