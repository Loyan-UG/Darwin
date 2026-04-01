import "server-only";
import { cookies } from "next/headers";

const STOREFRONT_PAYMENT_HANDOFF_COOKIE = "darwin-storefront-payment-handoff";

export type StorefrontPaymentHandoffState = {
  orderId: string;
  orderNumber?: string;
  paymentId: string;
  provider?: string;
  providerReference?: string;
  expiresAtUtc?: string;
};

function getCookieBaseOptions() {
  return {
    httpOnly: true,
    sameSite: "lax" as const,
    path: "/",
    secure: process.env.NODE_ENV === "production",
    maxAge: 60 * 30,
  };
}

export async function readStorefrontPaymentHandoff() {
  const cookieStore = await cookies();
  const raw = cookieStore.get(STOREFRONT_PAYMENT_HANDOFF_COOKIE)?.value;
  if (!raw) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as StorefrontPaymentHandoffState;
    if (!parsed.orderId || !parsed.paymentId) {
      return null;
    }

    return parsed;
  } catch {
    return null;
  }
}

export async function writeStorefrontPaymentHandoff(
  state: StorefrontPaymentHandoffState,
) {
  const cookieStore = await cookies();
  cookieStore.set(
    STOREFRONT_PAYMENT_HANDOFF_COOKIE,
    JSON.stringify(state),
    getCookieBaseOptions(),
  );
}

export async function clearStorefrontPaymentHandoff() {
  const cookieStore = await cookies();
  cookieStore.delete(STOREFRONT_PAYMENT_HANDOFF_COOKIE);
}
