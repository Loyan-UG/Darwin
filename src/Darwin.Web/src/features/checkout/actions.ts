"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { clearStorefrontCartState } from "@/features/cart/cookies";
import {
  createPublicStorefrontPaymentIntent,
  placePublicStorefrontOrder,
} from "@/features/checkout/api/public-checkout";
import {
  buildCheckoutDraftSearch,
  isCheckoutAddressComplete,
  readCheckoutDraftFromFormData,
  toCheckoutAddress,
} from "@/features/checkout/helpers";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

function revalidateCheckoutPaths() {
  revalidatePath("/cart");
  revalidatePath("/checkout");
}

export async function placeStorefrontOrderAction(formData: FormData) {
  const cartId = String(formData.get("cartId") ?? "").trim();
  const shippingTotalMinor = Number(formData.get("shippingTotalMinor") ?? "0");
  const draft = readCheckoutDraftFromFormData(formData);

  if (!cartId || !Number.isFinite(shippingTotalMinor) || shippingTotalMinor < 0) {
    redirect(
      `/checkout${buildCheckoutDraftSearch(draft, {
        checkoutError: "Invalid checkout order request.",
      })}`,
    );
  }

  if (!isCheckoutAddressComplete(draft)) {
    redirect(
      `/checkout${buildCheckoutDraftSearch(draft, {
        checkoutError: "Shipping and billing address fields are incomplete.",
      })}`,
    );
  }

  const orderResult = await placePublicStorefrontOrder({
    cartId,
    billingAddress: toCheckoutAddress(draft),
    shippingAddress: toCheckoutAddress(draft),
    selectedShippingMethodId: draft.selectedShippingMethodId || undefined,
    shippingTotalMinor,
    culture: getSiteRuntimeConfig().culture,
  });

  if (!orderResult.data) {
    redirect(
      `/checkout${buildCheckoutDraftSearch(draft, {
        checkoutError: orderResult.message ?? "Order could not be placed.",
      })}`,
    );
  }

  await clearStorefrontCartState();
  revalidateCheckoutPaths();
  redirect(
    `/checkout/orders/${orderResult.data.orderId}/confirmation?orderNumber=${encodeURIComponent(orderResult.data.orderNumber)}&checkoutStatus=order-placed`,
  );
}

export async function createStorefrontPaymentIntentAction(formData: FormData) {
  const orderId = String(formData.get("orderId") ?? "").trim();
  const orderNumber = String(formData.get("orderNumber") ?? "").trim();

  if (!orderId) {
    redirect("/checkout?checkoutError=Missing%20order%20identifier.");
  }

  const paymentResult = await createPublicStorefrontPaymentIntent({
    orderId,
    orderNumber: orderNumber || undefined,
  });

  if (!paymentResult.data || !paymentResult.data.checkoutUrl) {
    const paymentError =
      paymentResult.message ?? "Hosted checkout could not be started.";
    const suffix = orderNumber
      ? `?orderNumber=${encodeURIComponent(orderNumber)}&paymentError=${encodeURIComponent(paymentError)}`
      : `?paymentError=${encodeURIComponent(paymentError)}`;
    redirect(`/checkout/orders/${orderId}/confirmation${suffix}`);
  }

  redirect(paymentResult.data.checkoutUrl);
}
