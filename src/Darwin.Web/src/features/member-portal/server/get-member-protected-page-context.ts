import "server-only";
import { createMemberProtectedPageLoader } from "@/features/member-portal/server/create-member-protected-page-loader";
import {
  getMemberEditorRouteContext,
  getMemberInvoiceDetailRouteContext,
  getMemberInvoicesRouteContext,
  getMemberOrderDetailRouteContext,
  getMemberOrdersRouteContext,
} from "@/features/member-portal/server/get-member-route-context";
import {
  summarizeMemberCollectionHealth,
  summarizeMemberDetailHealth,
  summarizeMemberEditorHealth,
} from "@/lib/route-health";

const getCachedMemberEditorPageContext = createMemberProtectedPageLoader({
  operation: "load-editor-page-context",
  thresholdMs: 275,
  getContext: (culture: string, route: string) => ({
    culture,
    route,
  }),
  getEntryRoute: (_culture: string, route: string) => route,
  summarizeAuthorized: (routeContext) => summarizeMemberEditorHealth(routeContext),
  loadRouteContext: async (culture: string) => getMemberEditorRouteContext(culture),
});

const getCachedMemberOrdersPageContext = createMemberProtectedPageLoader({
  operation: "load-orders-page-context",
  thresholdMs: 300,
  getContext: (culture: string, page: number, pageSize: number) => ({
    culture,
    route: "/orders",
    page,
    pageSize,
  }),
  getEntryRoute: () => "/orders",
  summarizeAuthorized: (routeContext) =>
    summarizeMemberCollectionHealth(routeContext),
  loadRouteContext: async (culture: string, page: number, pageSize: number) =>
    getMemberOrdersRouteContext(culture, page, pageSize),
});

const getCachedMemberInvoicesPageContext = createMemberProtectedPageLoader({
  operation: "load-invoices-page-context",
  thresholdMs: 300,
  getContext: (culture: string, page: number, pageSize: number) => ({
    culture,
    route: "/invoices",
    page,
    pageSize,
  }),
  getEntryRoute: () => "/invoices",
  summarizeAuthorized: (routeContext) =>
    summarizeMemberCollectionHealth(routeContext),
  loadRouteContext: async (culture: string, page: number, pageSize: number) =>
    getMemberInvoicesRouteContext(culture, page, pageSize),
});

const getCachedMemberOrderDetailPageContext = createMemberProtectedPageLoader({
  operation: "load-order-detail-page-context",
  thresholdMs: 300,
  getContext: (culture: string, id: string) => ({
    culture,
    route: "/orders/[id]",
    id,
  }),
  getEntryRoute: () => "/orders/[id]",
  summarizeAuthorized: (routeContext) => ({
    ...summarizeMemberCollectionHealth(routeContext),
    detail: summarizeMemberDetailHealth(routeContext.orderResult),
  }),
  loadRouteContext: async (culture: string, id: string) =>
    getMemberOrderDetailRouteContext(culture, id),
});

const getCachedMemberInvoiceDetailPageContext = createMemberProtectedPageLoader({
  operation: "load-invoice-detail-page-context",
  thresholdMs: 300,
  getContext: (culture: string, id: string) => ({
    culture,
    route: "/invoices/[id]",
    id,
  }),
  getEntryRoute: () => "/invoices/[id]",
  summarizeAuthorized: (routeContext) => ({
    ...summarizeMemberCollectionHealth(routeContext),
    detail: summarizeMemberDetailHealth(routeContext.invoiceResult),
  }),
  loadRouteContext: async (culture: string, id: string) =>
    getMemberInvoiceDetailRouteContext(culture, id),
});

export function getMemberEditorPageContext(culture: string, route: string) {
  return getCachedMemberEditorPageContext(culture, route);
}

export function getMemberOrdersPageContext(
  culture: string,
  page: number,
  pageSize: number,
) {
  return getCachedMemberOrdersPageContext(culture, page, pageSize);
}

export function getMemberInvoicesPageContext(
  culture: string,
  page: number,
  pageSize: number,
) {
  return getCachedMemberInvoicesPageContext(culture, page, pageSize);
}

export function getMemberOrderDetailPageContext(culture: string, id: string) {
  return getCachedMemberOrderDetailPageContext(culture, id);
}

export function getMemberInvoiceDetailPageContext(culture: string, id: string) {
  return getCachedMemberInvoiceDetailPageContext(culture, id);
}
