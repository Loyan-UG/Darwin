import "server-only";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import type { PublicApiFetchResult } from "@/lib/api/fetch-public-json";
import type {
  PublicCheckoutAddress,
  PublicCheckoutIntent,
  PublicStorefrontOrderConfirmation,
  PublicStorefrontPaymentIntent,
  PlaceOrderFromCartResponse,
} from "@/features/checkout/types";

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
        message: "Storefront checkout resource was not found.",
      };
    }

    if (!response.ok) {
      let detail = `Storefront checkout API returned status ${response.status}.`;
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
      message: "Storefront checkout API could not be reached.",
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

export async function getPublicStorefrontOrderConfirmation(
  orderId: string,
  orderNumber?: string,
) {
  const params = new URLSearchParams();
  if (orderNumber) {
    params.set("orderNumber", orderNumber);
  }

  const query = params.toString();
  return fetchCheckoutJson<PublicStorefrontOrderConfirmation>(
    `/api/v1/public/checkout/orders/${orderId}/confirmation${query ? `?${query}` : ""}`,
  );
}
