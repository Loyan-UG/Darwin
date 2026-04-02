"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { clearStorefrontCartState } from "@/features/cart/cookies";
import {
  createPublicStorefrontPaymentIntent,
  placePublicStorefrontOrder,
} from "@/features/checkout/api/public-checkout";
import { writeStorefrontPaymentHandoff } from "@/features/checkout/cookies";
import {
  buildCheckoutDraftSearch,
  isCheckoutAddressComplete,
  readNonNegativeIntegerFromFormData,
  readCheckoutDraftFromFormData,
  toCheckoutAddress,
} from "@/features/checkout/helpers";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { toLocalizedQueryMessage } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";

function revalidateCheckoutPaths() {
  revalidatePath("/cart");
  revalidatePath("/checkout");
}

export async function placeStorefrontOrderAction(formData: FormData) {
  const cartId = String(formData.get("cartId") ?? "").trim();
  const shippingTotalMinor = readNonNegativeIntegerFromFormData(
    formData,
    "shippingTotalMinor",
  );
  const draft = readCheckoutDraftFromFormData(formData);

  if (!cartId || shippingTotalMinor === null) {
    redirect(
      `/checkout${buildCheckoutDraftSearch(draft, {
        checkoutError: toLocalizedQueryMessage("checkoutInvalidOrderRequestMessage"),
      })}`,
    );
  }

  if (!isCheckoutAddressComplete(draft)) {
    redirect(
      `/checkout${buildCheckoutDraftSearch(draft, {
        checkoutError: toLocalizedQueryMessage(
          "checkoutAddressIncompleteErrorMessage",
        ),
      })}`,
    );
  }

  const orderResult = await placePublicStorefrontOrder({
    cartId,
    billingAddress: toCheckoutAddress(draft),
    shippingAddress: toCheckoutAddress(draft),
    selectedShippingMethodId: draft.selectedShippingMethodId || undefined,
    shippingTotalMinor,
    culture: await getRequestCulture(),
  });

  if (!orderResult.data) {
    redirect(
      `/checkout${buildCheckoutDraftSearch(draft, {
        checkoutError:
          orderResult.message ??
          toLocalizedQueryMessage("checkoutPlaceOrderFailedMessage"),
      })}`,
    );
  }

  await clearStorefrontCartState();
  revalidateCheckoutPaths();
  redirect(
    buildAppQueryPath(`/checkout/orders/${orderResult.data.orderId}/confirmation`, {
      orderNumber: orderResult.data.orderNumber,
      checkoutStatus: "order-placed",
    }),
  );
}

export async function createStorefrontPaymentIntentAction(formData: FormData) {
  const orderId = String(formData.get("orderId") ?? "").trim();
  const orderNumber = String(formData.get("orderNumber") ?? "").trim();

  if (!orderId) {
    redirect(
      buildAppQueryPath("/checkout", {
        checkoutError: toLocalizedQueryMessage("checkoutMissingOrderIdentifierMessage"),
      }),
    );
  }

  const paymentResult = await createPublicStorefrontPaymentIntent({
    orderId,
    orderNumber: orderNumber || undefined,
  });

  if (!paymentResult.data || !paymentResult.data.checkoutUrl) {
    const paymentError =
      paymentResult.message ??
      toLocalizedQueryMessage("checkoutHostedCheckoutStartFailedMessage");
    redirect(
      buildAppQueryPath(`/checkout/orders/${orderId}/confirmation`, {
        orderNumber: orderNumber || undefined,
        paymentError,
      }),
    );
  }

  await writeStorefrontPaymentHandoff({
    orderId: paymentResult.data.orderId,
    orderNumber: orderNumber || undefined,
    paymentId: paymentResult.data.paymentId,
    provider: paymentResult.data.provider,
    providerReference: paymentResult.data.providerReference,
    expiresAtUtc: paymentResult.data.expiresAtUtc,
  });

  redirect(paymentResult.data.checkoutUrl);
}
