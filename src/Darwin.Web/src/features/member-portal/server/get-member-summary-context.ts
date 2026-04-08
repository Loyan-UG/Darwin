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
  normalizePagingArgs,
} from "@/lib/route-context-normalization";
import {
  buildMemberSummaryFootprint,
  buildSharedContextBaseDiagnostics,
} from "@/lib/shared-context-diagnostics";
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
  getSuccessContext: (result) => {
    const summary = summarizeMemberIdentityHealth(result);

    return buildSharedContextBaseDiagnostics("member-summary", {
      extras: {
        ...summary,
        sharedContextFootprint: buildMemberSummaryFootprint({
          scope: "identity",
          primaryStatus: summary.profileStatus,
          secondaryStatus: summary.preferencesStatus,
          tertiaryStatus: summary.addressesStatus,
        }),
      },
    });
  },
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
  getSuccessContext: (result) => {
    const summary = summarizeMemberCommerceSummaryHealth(result);

    return buildSharedContextBaseDiagnostics("member-summary", {
      extras: {
        ...summary,
        sharedContextFootprint: buildMemberSummaryFootprint({
          scope: "commerce-summary",
          primaryStatus: summary.ordersStatus,
          secondaryStatus: summary.invoicesStatus,
          tertiaryStatus: summary.loyaltyStatus,
        }),
      },
    });
  },
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
  normalizeArgs: normalizePagingArgs,
  getContext: (page: number, pageSize: number) =>
    memberSummaryObservationContext("commerce-summary", {
      page,
      pageSize,
      collection: "orders",
    }),
  getSuccessContext: (result) => {
    const summary = summarizeMemberPagedCollectionHealth(result);

    return buildSharedContextBaseDiagnostics("member-summary", {
      hasCanonicalNormalization: true,
      extras: {
        ...summary,
        sharedContextFootprint: buildMemberSummaryFootprint({
          scope: "orders-page",
          primaryStatus: summary.status,
        }),
      },
    });
  },
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
  normalizeArgs: normalizePagingArgs,
  getContext: (page: number, pageSize: number) =>
    memberSummaryObservationContext("commerce-summary", {
      page,
      pageSize,
      collection: "invoices",
    }),
  getSuccessContext: (result) => {
    const summary = summarizeMemberPagedCollectionHealth(result);

    return buildSharedContextBaseDiagnostics("member-summary", {
      hasCanonicalNormalization: true,
      extras: {
        ...summary,
        sharedContextFootprint: buildMemberSummaryFootprint({
          scope: "invoices-page",
          primaryStatus: summary.status,
        }),
      },
    });
  },
  load: (page: number, pageSize: number) =>
    getCurrentMemberInvoices({
      page,
      pageSize,
    }),
});
