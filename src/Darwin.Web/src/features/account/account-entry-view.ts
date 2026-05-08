import type { ComponentProps } from "react";
import { type AccountHubPage } from "@/components/account/account-hub-page";
import { type MemberDashboardPage } from "@/components/account/member-dashboard-page";
import type { PublicStorefrontContext } from "@/features/storefront/public-storefront-context";
import {
  createStorefrontCartProps,
} from "@/features/storefront/route-projections";
import { sanitizeAppPath } from "@/lib/locale-routing";

type AccountHubProps = ComponentProps<typeof AccountHubPage>;
type MemberDashboardProps = ComponentProps<typeof MemberDashboardPage>;

type IdentityContext = {
  profileResult: {
    data?: MemberDashboardProps["profile"] | null;
    status: string;
  };
  preferencesResult: {
    data?: MemberDashboardProps["preferences"] | null;
    status: string;
  };
  addressesResult: {
    data?: MemberDashboardProps["addresses"] | null;
    status: string;
  };
};

type CommerceSummaryContext = {
  ordersResult: {
    data?: { items?: MemberDashboardProps["recentOrders"] } | null;
    status: string;
  };
  invoicesResult: {
    data?: { items?: MemberDashboardProps["recentInvoices"] } | null;
    status: string;
  };
  loyaltyOverviewResult: {
    data?: MemberDashboardProps["loyaltyOverview"] | null;
    status: string;
  };
};

type MemberRouteContext = {
  identityContext: IdentityContext;
  commerceSummaryContext: CommerceSummaryContext;
  loyaltyBusinessesResult: {
    data?: { items?: MemberDashboardProps["loyaltyBusinesses"] } | null;
    status: string;
  };
  storefrontContext: PublicStorefrontContext;
};

export type AccountEntryView =
  | {
      kind: "public";
      props: AccountHubProps;
    }
  | {
      kind: "member";
      props: MemberDashboardProps;
    };

export function buildAccountEntryView(options: {
  culture: string;
  returnPath?: string;
  session: MemberDashboardProps["session"] | null;
  publicRouteContext?: {
    storefrontContext: PublicStorefrontContext;
  } | null;
  memberRouteContext?: MemberRouteContext | null;
}): AccountEntryView {
  const { culture, returnPath, session, publicRouteContext, memberRouteContext } =
    options;

  if (!session || !memberRouteContext) {
    const storefrontProps = createStorefrontCartProps(
      publicRouteContext!.storefrontContext,
    );

    return {
      kind: "public",
      props: {
        culture,
        ...storefrontProps,
        returnPath: sanitizeAppPath(returnPath, "/account"),
      },
    };
  }

  const {
    identityContext,
    commerceSummaryContext,
    loyaltyBusinessesResult,
    storefrontContext,
  } = memberRouteContext;
  const storefrontProps = createStorefrontCartProps(storefrontContext);

  return {
    kind: "member",
    props: {
      culture,
      session,
      profile: identityContext.profileResult.data ?? null,
      profileStatus: identityContext.profileResult.status,
      preferences: identityContext.preferencesResult.data ?? null,
      preferencesStatus: identityContext.preferencesResult.status,
      addresses: identityContext.addressesResult.data ?? [],
      addressesStatus: identityContext.addressesResult.status,
      recentOrders: commerceSummaryContext.ordersResult.data?.items ?? [],
      recentOrdersStatus: commerceSummaryContext.ordersResult.status,
      recentInvoices: commerceSummaryContext.invoicesResult.data?.items ?? [],
      recentInvoicesStatus: commerceSummaryContext.invoicesResult.status,
      loyaltyOverview: commerceSummaryContext.loyaltyOverviewResult.data ?? null,
      loyaltyOverviewStatus: commerceSummaryContext.loyaltyOverviewResult.status,
      loyaltyBusinesses: loyaltyBusinessesResult.data?.items ?? [],
      loyaltyBusinessesStatus: loyaltyBusinessesResult.status,
      ...storefrontProps,
    },
  };
}
