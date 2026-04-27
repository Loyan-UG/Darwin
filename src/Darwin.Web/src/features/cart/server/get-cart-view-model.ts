import "server-only";
import {
  getAnonymousCartId,
  pruneCartDisplaySnapshots,
  readCartDisplaySnapshots,
} from "@/features/cart/cookies";
import { getPublicCart } from "@/features/cart/api/public-cart";
import type { CartDisplaySnapshot, PublicCartItemRow } from "@/features/cart/types";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeCartViewModelHealth } from "@/lib/route-health";

export type CartViewRow = PublicCartItemRow & {
  display: CartDisplaySnapshot | null;
};

export type CartViewModel = {
  anonymousId: string | null;
  status: string;
  message?: string;
  cart: {
    cartId: string;
    currency: string;
    items: CartViewRow[];
    subtotalNetMinor: number;
    vatTotalMinor: number;
    grandTotalGrossMinor: number;
    couponCode?: string | null;
  } | null;
};

const getCachedCartViewModel = createCachedObservedLoader({
  area: "cart",
  operation: "load-view-model",
  thresholdMs: 250,
  getContext: (anonymousId: string | null, culture: string) => ({
    anonymousCartState: anonymousId ? "present" : "missing",
    culture,
  }),
  getSuccessContext: summarizeCartViewModelHealth,
  load: async (anonymousId: string | null, culture: string): Promise<CartViewModel> => {
    if (!anonymousId) {
      return {
        anonymousId: null,
        status: "empty",
        cart: null,
      };
    }

    const [cartResult, displaySnapshots] = await Promise.all([
      getPublicCart(anonymousId, culture),
      readCartDisplaySnapshots(),
    ]);

    if (!cartResult.data) {
      return {
        anonymousId,
        status: cartResult.status,
        message: cartResult.message,
        cart: null,
      };
    }

    const activeVariantIds = cartResult.data.items.map((item) => item.variantId);
    await pruneCartDisplaySnapshots(activeVariantIds);

    return {
      anonymousId,
      status: cartResult.status,
      cart: {
        cartId: cartResult.data.cartId,
        currency: cartResult.data.currency,
        subtotalNetMinor: cartResult.data.subtotalNetMinor,
        vatTotalMinor: cartResult.data.vatTotalMinor,
        grandTotalGrossMinor: cartResult.data.grandTotalGrossMinor,
        couponCode: cartResult.data.couponCode,
        items: cartResult.data.items.map((item) => ({
          ...item,
          display:
            displaySnapshots.find((snapshot) => snapshot.variantId === item.variantId) ??
            null,
        })),
      },
    };
  },
});

export async function getCartViewModel(culture: string): Promise<CartViewModel> {
  const anonymousId = await getAnonymousCartId();
  return getCachedCartViewModel(anonymousId, culture);
}
