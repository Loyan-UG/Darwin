import "server-only";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import type { PublicApiFetchResult } from "@/lib/api/fetch-public-json";
import { buildQuerySuffix } from "@/lib/query-params";
import type {
  PublicCheckoutAddress,
  PublicCheckoutIntent,
  PublicStorefrontPaymentCompletion,
  PublicStorefrontOrderConfirmation,
  PublicStorefrontPaymentIntent,
  PlaceOrderFromCartResponse,
} from "@/features/checkout/types";
import { toLocalizedQueryMessage } from "@/localization";

async function fetchCheckoutJson<T>(
  path: string,
  init?: RequestInit,
): Promise<PublicApiFetchResult<T>> {
  const { webApiBaseUrl } = getSiteRuntimeConfig();

  try {
    const response = await fetch(`${webApiBaseUrl}${path}`, {
      ...init,
      cache: "no-store",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
        ...(init?.headers ?? {}),
      },
    });

    if (response.status === 404) {
      return {
        data: null,
        status: "not-found",
        message: toLocalizedQueryMessage("storefrontCheckoutNotFoundMessage"),
      };
    }

    if (!response.ok) {
      let detail = toLocalizedQueryMessage("storefrontCheckoutHttpErrorMessage");
      try {
        const problem = (await response.json()) as { detail?: string; title?: string };
        detail = problem.detail ?? problem.title ?? detail;
      } catch {
        // Keep the status-based detail.
      }

      return {
        data: null,
        status: "http-error",
        message: detail,
      };
    }

    return {
      data: (await response.json()) as T,
      status: "ok",
    };
  } catch {
    return {
      data: null,
      status: "network-error",
      message: toLocalizedQueryMessage("storefrontCheckoutNetworkErrorMessage"),
    };
  }
}

export async function createPublicCheckoutIntent(input: {
  cartId: string;
  shippingAddress: PublicCheckoutAddress;
  selectedShippingMethodId?: string;
}) {
  return fetchCheckoutJson<PublicCheckoutIntent>("/api/v1/public/checkout/intent", {
    method: "POST",
    body: JSON.stringify({
      cartId: input.cartId,
      shippingAddress: input.shippingAddress,
      selectedShippingMethodId: input.selectedShippingMethodId || undefined,
    }),
  });
}

export async function placePublicStorefrontOrder(input: {
  cartId: string;
  billingAddress: PublicCheckoutAddress;
  shippingAddress: PublicCheckoutAddress;
  selectedShippingMethodId?: string;
  shippingTotalMinor: number;
  culture: string;
}) {
  return fetchCheckoutJson<PlaceOrderFromCartResponse>("/api/v1/public/checkout/orders", {
    method: "POST",
    body: JSON.stringify({
      cartId: input.cartId,
      billingAddress: input.billingAddress,
      shippingAddress: input.shippingAddress,
      selectedShippingMethodId: input.selectedShippingMethodId || undefined,
      shippingTotalMinor: input.shippingTotalMinor,
      culture: input.culture,
    }),
  });
}

export async function createPublicStorefrontPaymentIntent(input: {
  orderId: string;
  orderNumber?: string;
  provider?: string;
}) {
  return fetchCheckoutJson<PublicStorefrontPaymentIntent>(
    `/api/v1/public/checkout/orders/${input.orderId}/payment-intent`,
    {
      method: "POST",
      body: JSON.stringify({
        orderNumber: input.orderNumber || undefined,
        provider: input.provider || undefined,
      }),
    },
  );
}

export async function completePublicStorefrontPayment(input: {
  orderId: string;
  paymentId: string;
  orderNumber?: string;
  providerReference?: string;
  outcome: "Succeeded" | "Cancelled" | "Failed";
  failureReason?: string;
}) {
  return fetchCheckoutJson<PublicStorefrontPaymentCompletion>(
    `/api/v1/public/checkout/orders/${input.orderId}/payments/${input.paymentId}/complete`,
    {
      method: "POST",
      body: JSON.stringify({
        orderNumber: input.orderNumber || undefined,
        providerReference: input.providerReference || undefined,
        outcome: input.outcome,
        failureReason: input.failureReason || undefined,
      }),
    },
  );
}

export async function getPublicStorefrontOrderConfirmation(
  orderId: string,
  orderNumber?: string,
) {
  return fetchCheckoutJson<PublicStorefrontOrderConfirmation>(
    `/api/v1/public/checkout/orders/${orderId}/confirmation${buildQuerySuffix({
      orderNumber,
    })}`,
  );
}
