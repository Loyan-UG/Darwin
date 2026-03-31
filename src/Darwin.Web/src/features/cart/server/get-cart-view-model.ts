import "server-only";
import {
  getAnonymousCartId,
  pruneCartDisplaySnapshots,
  readCartDisplaySnapshots,
} from "@/features/cart/cookies";
import { getPublicCart } from "@/features/cart/api/public-cart";
import type { CartDisplaySnapshot, PublicCartItemRow } from "@/features/cart/types";

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

export async function getCartViewModel(): Promise<CartViewModel> {
  const anonymousId = await getAnonymousCartId();
  if (!anonymousId) {
    return {
      anonymousId: null,
      status: "empty",
      cart: null,
    };
  }

  const [cartResult, displaySnapshots] = await Promise.all([
    getPublicCart(anonymousId),
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
}
