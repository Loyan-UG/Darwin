import "server-only";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import type { PublicApiFetchResult } from "@/lib/api/fetch-public-json";
import type { PublicCartSummary } from "@/features/cart/types";

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
        message: "Storefront cart resource was not found.",
      };
    }

    if (!response.ok) {
      let detail = `Storefront cart API returned status ${response.status}.`;
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
      message: "Storefront cart API could not be reached.",
    };
  }
}

export async function getPublicCart(anonymousId: string) {
  const params = new URLSearchParams({
    anonymousId,
  });

  return fetchCartJson<PublicCartSummary>(
    `/api/v1/public/cart?${params.toString()}`,
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
