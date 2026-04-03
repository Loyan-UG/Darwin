import "server-only";
import { cache } from "react";
import {
  getCurrentMemberAddresses,
  getCurrentMemberCustomerContext,
  getCurrentMemberInvoices,
  getCurrentMemberLoyaltyOverview,
  getCurrentMemberOrders,
  getCurrentMemberPreferences,
  getCurrentMemberProfile,
} from "@/features/member-portal/api/member-portal";

export const getMemberIdentityContext = cache(async () =>
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
  ));

export const getMemberCommerceSummaryContext = cache(async () =>
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
  })));
