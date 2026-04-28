import "server-only";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import type { PublicApiFetchResult } from "@/lib/api/fetch-public-json";
import {
  createDiagnostics,
  getResponseDiagnostics,
  logApiFailure,
  withFailureDiagnostics,
} from "@/lib/api-diagnostics";
import { serializeQueryParams } from "@/lib/query-params";
import type { PublicCartSummary } from "@/features/cart/types";
import {
  resolveProblemQueryMessage,
  toLocalizedQueryMessage,
} from "@/localization";
import { buildWebApiFetchInit } from "@/lib/webapi-fetch";

async function fetchCartJson<T>(
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

    const diagnostics = getResponseDiagnostics("storefront-cart", path, response);

    if (response.status === 404) {
      const failureDiagnostics = withFailureDiagnostics(diagnostics, "not-found");
      return {
        data: null,
        status: "not-found",
        message: toLocalizedQueryMessage("storefrontCartNotFoundMessage"),
        diagnostics: failureDiagnostics,
      };
    }

    if (!response.ok) {
      let detail = toLocalizedQueryMessage("storefrontCartHttpErrorMessage");
      try {
        const problem = (await response.json()) as { detail?: string; title?: string };
        detail = resolveProblemQueryMessage(problem, "storefrontCartHttpErrorMessage");
      } catch {
        // Keep the status-based message.
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
        message: toLocalizedQueryMessage("storefrontCartInvalidPayloadMessage"),
        diagnostics: failureDiagnostics,
      };
    }
  } catch (error) {
    const diagnostics = withFailureDiagnostics(
      createDiagnostics("storefront-cart", path),
      "network-error",
    );
    logApiFailure(diagnostics, error);
    return {
      data: null,
      status: "network-error",
      message: toLocalizedQueryMessage("storefrontCartNetworkErrorMessage"),
      diagnostics,
    };
  }
}

export async function getPublicCart(anonymousId: string, culture?: string) {
  return fetchCartJson<PublicCartSummary>(
    `/api/v1/public/cart?${serializeQueryParams({ anonymousId, culture })}`,
  );
}

export async function addItemToPublicCart(input: {
  anonymousId: string;
  variantId: string;
  quantity: number;
  selectedAddOnValueIds?: string[];
}) {
  return fetchCartJson<PublicCartSummary>("/api/v1/public/cart/items", {
    method: "POST",
    body: JSON.stringify({
      anonymousId: input.anonymousId,
      variantId: input.variantId,
      quantity: input.quantity,
      selectedAddOnValueIds: input.selectedAddOnValueIds ?? [],
    }),
  });
}

export async function updatePublicCartItem(input: {
  cartId: string;
  variantId: string;
  quantity: number;
  selectedAddOnValueIdsJson?: string;
}) {
  return fetchCartJson<PublicCartSummary>("/api/v1/public/cart/items", {
    method: "PUT",
    body: JSON.stringify({
      cartId: input.cartId,
      variantId: input.variantId,
      quantity: input.quantity,
      selectedAddOnValueIdsJson: input.selectedAddOnValueIdsJson,
    }),
  });
}

export async function removePublicCartItem(input: {
  cartId: string;
  variantId: string;
  selectedAddOnValueIdsJson?: string;
}) {
  return fetchCartJson<PublicCartSummary>("/api/v1/public/cart/items", {
    method: "DELETE",
    body: JSON.stringify({
      cartId: input.cartId,
      variantId: input.variantId,
      selectedAddOnValueIdsJson: input.selectedAddOnValueIdsJson,
    }),
  });
}

export async function applyPublicCartCoupon(input: {
  cartId: string;
  couponCode?: string;
}) {
  return fetchCartJson<PublicCartSummary>("/api/v1/public/cart/coupon", {
    method: "POST",
    body: JSON.stringify({
      cartId: input.cartId,
      couponCode: input.couponCode ?? null,
    }),
  });
}
