import "server-only";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import type { PublicApiFetchResult } from "@/lib/api/fetch-public-json";
import { serializeQueryParams } from "@/lib/query-params";
import type { PublicCartSummary } from "@/features/cart/types";
import { toLocalizedQueryMessage } from "@/localization";

async function fetchCartJson<T>(
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
        message: toLocalizedQueryMessage("storefrontCartNotFoundMessage"),
      };
    }

    if (!response.ok) {
      let detail = toLocalizedQueryMessage("storefrontCartHttpErrorMessage");
      try {
        const problem = (await response.json()) as { detail?: string; title?: string };
        detail = problem.detail ?? problem.title ?? detail;
      } catch {
        // Keep the status-based message.
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
      message: toLocalizedQueryMessage("storefrontCartNetworkErrorMessage"),
    };
  }
}

export async function getPublicCart(anonymousId: string) {
  return fetchCartJson<PublicCartSummary>(
    `/api/v1/public/cart?${serializeQueryParams({ anonymousId })}`,
  );
}

export async function addItemToPublicCart(input: {
  anonymousId: string;
  variantId: string;
  quantity: number;
}) {
  return fetchCartJson<PublicCartSummary>("/api/v1/public/cart/items", {
    method: "POST",
    body: JSON.stringify({
      anonymousId: input.anonymousId,
      variantId: input.variantId,
      quantity: input.quantity,
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
