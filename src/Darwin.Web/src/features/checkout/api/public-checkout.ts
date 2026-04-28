import "server-only";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import type { PublicApiFetchResult } from "@/lib/api/fetch-public-json";
import {
  createDiagnostics,
  getResponseDiagnostics,
  logApiFailure,
  withFailureDiagnostics,
} from "@/lib/api-diagnostics";
import { buildQuerySuffix } from "@/lib/query-params";
import type {
  PublicCheckoutAddress,
  PublicCheckoutIntent,
  PublicStorefrontPaymentCompletion,
  PublicStorefrontOrderConfirmation,
  PublicStorefrontPaymentIntent,
  PlaceOrderFromCartResponse,
} from "@/features/checkout/types";
import {
  resolveProblemQueryMessage,
  toLocalizedQueryMessage,
} from "@/localization";
import { buildWebApiFetchInit } from "@/lib/webapi-fetch";

async function fetchCheckoutJson<T>(
  path: string,
  init?: RequestInit,
): Promise<PublicApiFetchResult<T>> {
  const { webApiBaseUrl } = getSiteRuntimeConfig();

  try {
    const requestUrl = `${webApiBaseUrl}${path}`;
    const response = await fetch(requestUrl, buildWebApiFetchInit(requestUrl, {
      ...init,
      cache: "no-store",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
        ...(init?.headers ?? {}),
      },
    }));

    const diagnostics = getResponseDiagnostics("storefront-checkout", path, response);

    if (response.status === 404) {
      const failureDiagnostics = withFailureDiagnostics(diagnostics, "not-found");
      return {
        data: null,
        status: "not-found",
        message: toLocalizedQueryMessage("storefrontCheckoutNotFoundMessage"),
        diagnostics: failureDiagnostics,
      };
    }

    if (!response.ok) {
      let detail = toLocalizedQueryMessage("storefrontCheckoutHttpErrorMessage");
      try {
        const problem = (await response.json()) as { detail?: string; title?: string };
        detail = resolveProblemQueryMessage(problem, "storefrontCheckoutHttpErrorMessage");
      } catch {
        // Keep the status-based detail.
      }

      const failureDiagnostics = withFailureDiagnostics(diagnostics, "http-error");
      logApiFailure(failureDiagnostics, detail);
      return {
        data: null,
        status: "http-error",
        message: detail,
        diagnostics: failureDiagnostics,
      };
    }

    try {
      return {
        data: (await response.json()) as T,
        status: "ok",
        diagnostics,
      };
    } catch (error) {
      const failureDiagnostics = withFailureDiagnostics(
        diagnostics,
        "invalid-payload",
      );
      logApiFailure(failureDiagnostics, error);
      return {
        data: null,
        status: "invalid-payload",
        message: toLocalizedQueryMessage("storefrontCheckoutInvalidPayloadMessage"),
        diagnostics: failureDiagnostics,
      };
    }
  } catch (error) {
    const diagnostics = withFailureDiagnostics(
      createDiagnostics("storefront-checkout", path),
      "network-error",
    );
    logApiFailure(diagnostics, error);
    return {
      data: null,
      status: "network-error",
      message: toLocalizedQueryMessage("storefrontCheckoutNetworkErrorMessage"),
      diagnostics,
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
    `/api/v1/public/checkout/orders/${encodeURIComponent(input.orderId)}/payment-intent`,
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
    `/api/v1/public/checkout/orders/${encodeURIComponent(input.orderId)}/payments/${encodeURIComponent(input.paymentId)}/complete`,
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
    `/api/v1/public/checkout/orders/${encodeURIComponent(orderId)}/confirmation${buildQuerySuffix({
      orderNumber,
    })}`,
  );
}
