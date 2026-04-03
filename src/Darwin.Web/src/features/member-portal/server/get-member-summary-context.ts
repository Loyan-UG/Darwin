import "server-only";
import {
  getCurrentMemberAddresses,
  getCurrentMemberCustomerContext,
  getCurrentMemberInvoices,
  getCurrentMemberLoyaltyOverview,
  getCurrentMemberOrders,
  getCurrentMemberPreferences,
  getCurrentMemberProfile,
} from "@/features/member-portal/api/member-portal";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import {
  summarizeMemberCommerceSummaryHealth,
  summarizeMemberIdentityHealth,
  summarizeMemberPagedCollectionHealth,
} from "@/lib/route-health";
import { memberSummaryObservationContext } from "@/lib/route-observation-context";

export const getMemberIdentityContext = createCachedObservedLoader({
  area: "member-summary",
  operation: "load-identity-context",
  thresholdMs: 250,
  getContext: () => memberSummaryObservationContext("identity"),
  getSuccessContext: summarizeMemberIdentityHealth,
  load: () =>
    Promise.all([
      getCurrentMemberProfile(),
      getCurrentMemberPreferences(),
      getCurrentMemberCustomerContext(),
      getCurrentMemberAddresses(),
    ]).then(
      ([
        profileResult,
        preferencesResult,
        customerContextResult,
        addressesResult,
      ]) => ({
        profileResult,
        preferencesResult,
        customerContextResult,
        addressesResult,
      }),
    ),
});

export const getMemberCommerceSummaryContext = createCachedObservedLoader({
  area: "member-summary",
  operation: "load-commerce-summary",
  thresholdMs: 250,
  getContext: () => memberSummaryObservationContext("commerce-summary"),
  getSuccessContext: summarizeMemberCommerceSummaryHealth,
  load: () =>
    Promise.all([
      getCurrentMemberOrders({
        page: 1,
        pageSize: 3,
      }),
      getCurrentMemberInvoices({
        page: 1,
        pageSize: 3,
      }),
      getCurrentMemberLoyaltyOverview(),
    ]).then(([ordersResult, invoicesResult, loyaltyOverviewResult]) => ({
      ordersResult,
      invoicesResult,
      loyaltyOverviewResult,
    })),
});

export const getMemberOrdersPageContext = createCachedObservedLoader({
  area: "member-summary",
  operation: "load-orders-page",
  thresholdMs: 250,
  getContext: (page: number, pageSize: number) =>
    memberSummaryObservationContext("commerce-summary", {
      page,
      pageSize,
      collection: "orders",
    }),
  getSuccessContext: summarizeMemberPagedCollectionHealth,
  load: (page: number, pageSize: number) =>
    getCurrentMemberOrders({
      page,
      pageSize,
    }),
});

export const getMemberInvoicesPageContext = createCachedObservedLoader({
  area: "member-summary",
  operation: "load-invoices-page",
  thresholdMs: 250,
  getContext: (page: number, pageSize: number) =>
    memberSummaryObservationContext("commerce-summary", {
      page,
      pageSize,
      collection: "invoices",
    }),
  getSuccessContext: summarizeMemberPagedCollectionHealth,
  load: (page: number, pageSize: number) =>
    getCurrentMemberInvoices({
      page,
      pageSize,
    }),
});
