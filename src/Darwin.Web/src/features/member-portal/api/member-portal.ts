import "server-only";
import type { BusinessDetailWithMyAccount } from "@/features/businesses/types";
import { getFreshMemberAccessToken } from "@/features/member-session/server";
import { buildQuerySuffix, serializeQueryParams } from "@/lib/query-params";
import type {
  LinkedCustomerContext,
  MemberAddress,
  MemberCustomerProfile,
  MemberInvoiceDetail,
  MemberInvoiceSummary,
  MemberOrderDetail,
  MemberOrderSummary,
  MemberPreferences,
  LoyaltyAccountSummary,
  LoyaltyScanMode,
  PreparedMemberLoyaltyScanSession,
  LoyaltyBusinessDashboard,
  MyLoyaltyBusinessSummary,
  MyPromotionsResponse,
  LoyaltyRewardSummary,
  LoyaltyTimelinePage,
  MyLoyaltyOverview,
  PagedResponse,
} from "@/features/member-portal/types";
import { toLocalizedQueryMessage } from "@/localization";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

export type MemberApiFetchStatus =
  | "ok"
  | "unauthenticated"
  | "unauthorized"
  | "not-found"
  | "network-error"
  | "http-error"
  | "invalid-payload";

export type MemberApiFetchResult<T> = {
  data: T | null;
  status: MemberApiFetchStatus;
  message?: string;
};

async function fetchMemberJson<T>(
  path: string,
): Promise<MemberApiFetchResult<T>> {
  const accessToken = await getFreshMemberAccessToken();
  if (!accessToken) {
    return {
      data: null,
      status: "unauthenticated",
      message: toLocalizedQueryMessage("memberSessionRequiredMessage"),
    };
  }

  const { webApiBaseUrl } = getSiteRuntimeConfig();

  try {
    const runFetch = async (token: string) =>
      fetch(`${webApiBaseUrl}${path}`, {
        cache: "no-store",
        headers: {
          Accept: "application/json",
          Authorization: `Bearer ${token}`,
        },
      });

    let response = await runFetch(accessToken);

    if (response.status === 401 || response.status === 403) {
      const refreshedAccessToken = await getFreshMemberAccessToken(true);
      if (refreshedAccessToken) {
        response = await runFetch(refreshedAccessToken);
      }
    }

    if (response.status === 401 || response.status === 403) {
      return {
        data: null,
        status: "unauthorized",
        message: toLocalizedQueryMessage("memberSessionUnauthorizedMessage"),
      };
    }

    if (response.status === 404) {
      return {
        data: null,
        status: "not-found",
        message: toLocalizedQueryMessage("memberResourceNotFoundMessage"),
      };
    }

    if (!response.ok) {
      let detail = toLocalizedQueryMessage("memberApiHttpErrorMessage");
      try {
        const problem = (await response.json()) as { detail?: string; title?: string };
        detail = problem.detail ?? problem.title ?? detail;
      } catch {
        // Keep status-based detail.
      }

      return {
        data: null,
        status: "http-error",
        message: detail,
      };
    }

    try {
      return {
        data: (await response.json()) as T,
        status: "ok",
      };
    } catch {
      return {
        data: null,
        status: "invalid-payload",
        message: toLocalizedQueryMessage("memberApiInvalidPayloadMessage"),
      };
    }
  } catch {
    return {
      data: null,
      status: "network-error",
      message: toLocalizedQueryMessage("memberApiNetworkErrorMessage"),
    };
  }
}

function buildPagedQuery(page?: number, pageSize?: number) {
  return buildQuerySuffix({
    page: String(page ?? 1),
    pageSize: String(pageSize ?? 20),
  });
}

async function mutateMemberJson<T>(
  path: string,
  init: RequestInit,
): Promise<MemberApiFetchResult<T>> {
  const accessToken = await getFreshMemberAccessToken();
  if (!accessToken) {
    return {
      data: null,
      status: "unauthenticated",
      message: toLocalizedQueryMessage("memberSessionRequiredMessage"),
    };
  }

  const { webApiBaseUrl } = getSiteRuntimeConfig();

  try {
    const runFetch = async (token: string) =>
      fetch(`${webApiBaseUrl}${path}`, {
        ...init,
        cache: "no-store",
        headers: {
          Accept: "application/json",
          ...(init.body ? { "Content-Type": "application/json" } : {}),
          Authorization: `Bearer ${token}`,
          ...(init.headers ?? {}),
        },
      });

    let response = await runFetch(accessToken);
    if (response.status === 401 || response.status === 403) {
      const refreshedAccessToken = await getFreshMemberAccessToken(true);
      if (refreshedAccessToken) {
        response = await runFetch(refreshedAccessToken);
      }
    }

    if (response.status === 401 || response.status === 403) {
      return {
        data: null,
        status: "unauthorized",
        message: toLocalizedQueryMessage("memberSessionUnauthorizedMessage"),
      };
    }

    if (response.status === 404) {
      return {
        data: null,
        status: "not-found",
        message: toLocalizedQueryMessage("memberResourceNotFoundMessage"),
      };
    }

    if (!response.ok) {
      let detail = toLocalizedQueryMessage("memberApiHttpErrorMessage");
      try {
        const problem = (await response.json()) as { detail?: string; title?: string };
        detail = problem.detail ?? problem.title ?? detail;
      } catch {
        // Keep status-based detail.
      }

      return {
        data: null,
        status: "http-error",
        message: detail,
      };
    }

    if (response.status === 204 || response.headers.get("content-length") === "0") {
      return {
        data: null,
        status: "ok",
      };
    }

    try {
      return {
        data: (await response.json()) as T,
        status: "ok",
      };
    } catch {
      return {
        data: null,
        status: "ok",
      };
    }
  } catch {
    return {
      data: null,
      status: "network-error",
      message: toLocalizedQueryMessage("memberApiNetworkErrorMessage"),
    };
  }
}

export async function getCurrentMemberProfile() {
  return fetchMemberJson<MemberCustomerProfile>("/api/v1/member/profile/me");
}

export async function getCurrentMemberPreferences() {
  return fetchMemberJson<MemberPreferences>("/api/v1/member/profile/preferences");
}

export async function getCurrentMemberCustomerContext() {
  return fetchMemberJson<LinkedCustomerContext>(
    "/api/v1/member/profile/customer/context",
  );
}

export async function getCurrentMemberAddresses() {
  return fetchMemberJson<MemberAddress[]>("/api/v1/member/profile/addresses");
}

export async function getCurrentMemberOrders(input?: {
  page?: number;
  pageSize?: number;
}) {
  return fetchMemberJson<PagedResponse<MemberOrderSummary>>(
    `/api/v1/member/orders${buildPagedQuery(input?.page, input?.pageSize)}`,
  );
}

export async function getCurrentMemberOrder(id: string) {
  return fetchMemberJson<MemberOrderDetail>(`/api/v1/member/orders/${id}`);
}

export async function getCurrentMemberInvoices(input?: {
  page?: number;
  pageSize?: number;
}) {
  return fetchMemberJson<PagedResponse<MemberInvoiceSummary>>(
    `/api/v1/member/invoices${buildPagedQuery(input?.page, input?.pageSize)}`,
  );
}

export async function getCurrentMemberInvoice(id: string) {
  return fetchMemberJson<MemberInvoiceDetail>(`/api/v1/member/invoices/${id}`);
}

export async function getCurrentMemberLoyaltyOverview() {
  return fetchMemberJson<MyLoyaltyOverview>("/api/v1/member/loyalty/my/overview");
}

export async function getCurrentMemberLoyaltyBusinesses(input?: {
  page?: number;
  pageSize?: number;
  includeInactiveBusinesses?: boolean;
}) {
  return fetchMemberJson<PagedResponse<MyLoyaltyBusinessSummary>>(
    `/api/v1/member/loyalty/my/businesses?${serializeQueryParams({
      page: String(input?.page ?? 1),
      pageSize: String(input?.pageSize ?? 12),
      includeInactiveBusinesses: String(input?.includeInactiveBusinesses ?? false),
    })}`,
  );
}

export async function getCurrentMemberLoyaltyBusinessDashboard(businessId: string) {
  return fetchMemberJson<LoyaltyBusinessDashboard>(
    `/api/v1/member/loyalty/business/${businessId}/dashboard`,
  );
}

export async function getCurrentMemberLoyaltyRewards(businessId: string) {
  return fetchMemberJson<LoyaltyRewardSummary[]>(
    `/api/v1/member/loyalty/business/${businessId}/rewards`,
  );
}

export async function getCurrentMemberLoyaltyTimeline(input: {
  businessId: string;
  pageSize?: number;
  beforeAtUtc?: string | null;
  beforeId?: string | null;
}) {
  return mutateMemberJson<LoyaltyTimelinePage>("/api/v1/member/loyalty/my/timeline", {
    method: "POST",
    body: JSON.stringify({
      businessId: input.businessId,
      pageSize: input.pageSize ?? 10,
      beforeAtUtc: input.beforeAtUtc ?? null,
      beforeId: input.beforeId ?? null,
    }),
  });
}

export async function getCurrentMemberPromotions(input?: {
  businessId?: string;
  maxItems?: number;
}) {
  return mutateMemberJson<MyPromotionsResponse>("/api/v1/member/loyalty/my/promotions", {
    method: "POST",
    body: JSON.stringify({
      businessId: input?.businessId ?? null,
      maxItems: input?.maxItems ?? 6,
    }),
  });
}

export async function getCurrentMemberBusinessWithMyAccount(businessId: string) {
  return fetchMemberJson<BusinessDetailWithMyAccount>(
    `/api/v1/member/businesses/${businessId}/with-my-account`,
  );
}

export async function joinCurrentMemberLoyaltyBusiness(input: {
  businessId: string;
  businessLocationId?: string | null;
}) {
  return mutateMemberJson<LoyaltyAccountSummary>(
    `/api/v1/member/loyalty/account/${input.businessId}/join`,
    {
      method: "POST",
      body: JSON.stringify({
        businessLocationId: input.businessLocationId ?? null,
      }),
    },
  );
}

export async function prepareCurrentMemberLoyaltyScanSession(input: {
  businessId: string;
  businessLocationId?: string | null;
  mode: LoyaltyScanMode;
  selectedRewardTierIds?: string[];
  deviceId?: string | null;
}) {
  return mutateMemberJson<PreparedMemberLoyaltyScanSession>(
    "/api/v1/member/loyalty/scan/prepare",
    {
      method: "POST",
      body: JSON.stringify({
        businessId: input.businessId,
        businessLocationId: input.businessLocationId ?? null,
        mode: input.mode,
        selectedRewardTierIds: input.selectedRewardTierIds ?? [],
        deviceId: input.deviceId ?? null,
      }),
    },
  );
}

export async function trackCurrentMemberPromotionInteraction(input: {
  businessId: string;
  businessName: string;
  title: string;
  ctaKind: string;
  eventType: "Impression" | "Open" | "Claim";
  occurredAtUtc?: string | null;
}) {
  return mutateMemberJson<never>("/api/v1/member/loyalty/my/promotions/track", {
    method: "POST",
    body: JSON.stringify({
      businessId: input.businessId,
      businessName: input.businessName,
      title: input.title,
      ctaKind: input.ctaKind,
      eventType: input.eventType,
      occurredAtUtc: input.occurredAtUtc ?? null,
    }),
  });
}

export async function updateCurrentMemberProfile(input: {
  id: string;
  email?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  phoneE164?: string | null;
  locale?: string | null;
  timezone?: string | null;
  currency?: string | null;
  rowVersion: string;
}) {
  return mutateMemberJson<never>("/api/v1/member/profile/me", {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export async function requestCurrentMemberPhoneVerification(input: {
  channel?: string | null;
}) {
  return mutateMemberJson<never>("/api/v1/member/profile/me/phone/request-verification", {
    method: "POST",
    body: JSON.stringify({
      channel: input.channel ?? null,
    }),
  });
}

export async function confirmCurrentMemberPhoneVerification(input: {
  code: string;
}) {
  return mutateMemberJson<never>("/api/v1/member/profile/me/phone/confirm", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function updateCurrentMemberPreferences(input: {
  rowVersion: string;
  marketingConsent: boolean;
  allowEmailMarketing: boolean;
  allowSmsMarketing: boolean;
  allowWhatsAppMarketing: boolean;
  allowPromotionalPushNotifications: boolean;
  allowOptionalAnalyticsTracking: boolean;
}) {
  return mutateMemberJson<never>("/api/v1/member/profile/preferences", {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export async function changeCurrentMemberPassword(input: {
  currentPassword: string;
  newPassword: string;
}) {
  return mutateMemberJson<never>("/api/v1/auth/password/change", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function createCurrentMemberAddress(input: {
  fullName: string;
  company?: string | null;
  street1: string;
  street2?: string | null;
  postalCode: string;
  city: string;
  state?: string | null;
  countryCode: string;
  phoneE164?: string | null;
  isDefaultBilling: boolean;
  isDefaultShipping: boolean;
}) {
  return mutateMemberJson<MemberAddress>("/api/v1/member/profile/addresses", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function updateCurrentMemberAddress(
  id: string,
  input: {
    rowVersion: string;
    fullName: string;
    company?: string | null;
    street1: string;
    street2?: string | null;
    postalCode: string;
    city: string;
    state?: string | null;
    countryCode: string;
    phoneE164?: string | null;
    isDefaultBilling: boolean;
    isDefaultShipping: boolean;
  },
) {
  return mutateMemberJson<MemberAddress>(`/api/v1/member/profile/addresses/${id}`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export async function deleteCurrentMemberAddress(id: string, rowVersion: string) {
  return mutateMemberJson<never>(
    `/api/v1/member/profile/addresses/${id}/delete`,
    {
      method: "POST",
      body: JSON.stringify({
        rowVersion,
      }),
    },
  );
}

export async function setCurrentMemberAddressDefault(
  id: string,
  input: {
    asBilling: boolean;
    asShipping: boolean;
  },
) {
  return mutateMemberJson<MemberAddress>(
    `/api/v1/member/profile/addresses/${id}/default`,
    {
      method: "POST",
      body: JSON.stringify(input),
    },
  );
}
