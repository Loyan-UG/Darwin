import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { completePublicStorefrontPayment } from "@/features/checkout/api/public-checkout";
import {
  clearStorefrontPaymentHandoff,
  readStorefrontPaymentHandoff,
} from "@/features/checkout/cookies";

type FinalizeRouteContext = {
  params: Promise<{
    orderId: string;
  }>;
};

function buildRedirectUrl(request: NextRequest, orderId: string) {
  return new URL(`/checkout/orders/${orderId}/confirmation`, request.url);
}

function resolveOutcome(searchParams: URLSearchParams) {
  const explicit = searchParams.get("outcome");
  if (
    explicit === "Succeeded" ||
    explicit === "Cancelled" ||
    explicit === "Failed"
  ) {
    return explicit;
  }

  if (searchParams.get("cancelled") === "true") {
    return "Cancelled";
  }

  return "Succeeded";
}

export async function GET(request: NextRequest, context: FinalizeRouteContext) {
  const { orderId } = await context.params;
  const redirectUrl = buildRedirectUrl(request, orderId);
  const searchParams = request.nextUrl.searchParams;
  const orderNumber = searchParams.get("orderNumber")?.trim() || undefined;
  const handoff = await readStorefrontPaymentHandoff();

  if (!handoff || handoff.orderId !== orderId || !handoff.paymentId) {
    redirectUrl.searchParams.set("paymentCompletionStatus", "missing-context");
    if (orderNumber) {
      redirectUrl.searchParams.set("orderNumber", orderNumber);
    }
    if (searchParams.get("cancelled") === "true") {
      redirectUrl.searchParams.set("cancelled", "true");
    }
    return NextResponse.redirect(redirectUrl);
  }

  const outcome = resolveOutcome(searchParams);
  const failureReason =
    searchParams.get("failureReason")?.trim() ||
    (outcome === "Cancelled"
      ? "Shopper cancelled hosted checkout."
      : undefined);
  const result = await completePublicStorefrontPayment({
    orderId,
    paymentId: handoff.paymentId,
    orderNumber: orderNumber ?? handoff.orderNumber,
    providerReference:
      searchParams.get("providerReference")?.trim() ||
      handoff.providerReference,
    outcome,
    failureReason,
  });

  await clearStorefrontPaymentHandoff();

  if (orderNumber ?? handoff.orderNumber) {
    redirectUrl.searchParams.set(
      "orderNumber",
      orderNumber ?? handoff.orderNumber ?? "",
    );
  }

  if (outcome === "Cancelled") {
    redirectUrl.searchParams.set("cancelled", "true");
  }

  if (result.status !== "ok" || !result.data) {
    redirectUrl.searchParams.set("paymentCompletionStatus", "failed");
    redirectUrl.searchParams.set(
      "paymentError",
      result.message ?? "Hosted checkout return could not be reconciled.",
    );
    return NextResponse.redirect(redirectUrl);
  }

  redirectUrl.searchParams.set("paymentCompletionStatus", "completed");
  redirectUrl.searchParams.set("paymentOutcome", outcome);
  redirectUrl.searchParams.set("paymentStatus", result.data.paymentStatus);
  redirectUrl.searchParams.set("orderStatus", result.data.orderStatus);

  return NextResponse.redirect(redirectUrl);
}
