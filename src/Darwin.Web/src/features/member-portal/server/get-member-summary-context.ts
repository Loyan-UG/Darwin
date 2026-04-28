import "server-only";
import {
  getCurrentMemberAddresses,
  getCurrentMemberCustomerContext,
  getCurrentMemberInvoices,
  getCurrentMemberLoyaltyOverviewForCulture,
  getCurrentMemberOrders,
  getCurrentMemberPreferences,
  getCurrentMemberProfile,
} from "@/features/member-portal/api/member-portal";
import { createSharedContextLoader } from "@/lib/shared-context-loader";
import {
  normalizeCultureArg,
  normalizePagingArgs,
} from "@/lib/route-context-normalization";
import {
  buildMemberSummaryFootprint,
} from "@/lib/shared-context-diagnostics";
import {
  summarizeMemberCommerceSummaryHealth,
  summarizeMemberIdentityHealth,
  summarizeMemberPagedCollectionHealth,
} from "@/lib/route-health";
import { memberSummaryObservationContext } from "@/lib/route-observation-context";

export const getMemberIdentityContext = createSharedContextLoader({
  kind: "member-summary",
  area: "member-summary",
  operation: "load-identity-context",
  getContext: () => memberSummaryObservationContext("identity"),
  getSuccessContext: (result) => {
    const summary = summarizeMemberIdentityHealth(result);
    return {
      ...summary,
      sharedContextFootprint: buildMemberSummaryFootprint({
        scope: "identity",
        primaryStatus: summary.profileStatus,
        secondaryStatus: summary.preferencesStatus,
        tertiaryStatus: summary.addressesStatus,
      }),
    };
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

export const getMemberCommerceSummaryContext = createSharedContextLoader({
  kind: "member-summary",
  area: "member-summary",
  operation: "load-commerce-summary",
  normalizeArgs: normalizeCultureArg,
  getContext: (culture: string) =>
    memberSummaryObservationContext("commerce-summary", { culture }),
  getSuccessContext: (result) => {
    const summary = summarizeMemberCommerceSummaryHealth(result);
    return {
      ...summary,
      sharedContextFootprint: buildMemberSummaryFootprint({
        scope: "commerce-summary",
        primaryStatus: summary.ordersStatus,
        secondaryStatus: summary.invoicesStatus,
        tertiaryStatus: summary.loyaltyStatus,
      }),
    };
  },
  load: (culture: string) =>
    Promise.all([
      getCurrentMemberOrders({
        page: 1,
        pageSize: 3,
        culture,
      }),
      getCurrentMemberInvoices({
        page: 1,
        pageSize: 3,
        culture,
      }),
      getCurrentMemberLoyaltyOverviewForCulture(culture),
    ]).then(([ordersResult, invoicesResult, loyaltyOverviewResult]) => ({
      ordersResult,
      invoicesResult,
      loyaltyOverviewResult,
    })),
});

export const getMemberOrdersPageContext = createSharedContextLoader({
  kind: "member-summary",
  area: "member-summary",
  operation: "load-orders-page",
  normalizeArgs: (culture: string, page: number, pageSize: number) =>
    [normalizeCultureArg(culture)[0], ...normalizePagingArgs(page, pageSize)] as [
      string,
      number,
      number,
    ],
  getContext: (culture: string, page: number, pageSize: number) =>
    memberSummaryObservationContext("commerce-summary", {
      culture,
      page,
      pageSize,
      collection: "orders",
    }),
  getSuccessContext: (result) => {
    const summary = summarizeMemberPagedCollectionHealth(result);
    return {
      ...summary,
      sharedContextFootprint: buildMemberSummaryFootprint({
        scope: "orders-page",
        primaryStatus: summary.status,
      }),
    };
  },
  load: (culture: string, page: number, pageSize: number) =>
    getCurrentMemberOrders({
      page,
      pageSize,
      culture,
    }),
});

export const getMemberInvoicesPageContext = createSharedContextLoader({
  kind: "member-summary",
  area: "member-summary",
  operation: "load-invoices-page",
  normalizeArgs: (culture: string, page: number, pageSize: number) =>
    [normalizeCultureArg(culture)[0], ...normalizePagingArgs(page, pageSize)] as [
      string,
      number,
      number,
    ],
  getContext: (culture: string, page: number, pageSize: number) =>
    memberSummaryObservationContext("commerce-summary", {
      culture,
      page,
      pageSize,
      collection: "invoices",
    }),
  getSuccessContext: (result) => {
    const summary = summarizeMemberPagedCollectionHealth(result);
    return {
      ...summary,
      sharedContextFootprint: buildMemberSummaryFootprint({
        scope: "invoices-page",
        primaryStatus: summary.status,
      }),
    };
  },
  load: (culture: string, page: number, pageSize: number) =>
    getCurrentMemberInvoices({
      page,
      pageSize,
      culture,
    }),
});
