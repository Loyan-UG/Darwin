import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { completePublicStorefrontPayment } from "@/features/checkout/api/public-checkout";
import {
  clearStorefrontPaymentHandoff,
  readStorefrontPaymentHandoff,
} from "@/features/checkout/cookies";
import { readSearchTextParam } from "@/features/checkout/helpers";
import { buildAppQueryPath, buildLocalizedPath } from "@/lib/locale-routing";
import { toLocalizedQueryMessage } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";

type FinalizeRouteContext = {
  params: Promise<{
    orderId: string;
  }>;
};

type FinalizeOutcome = "Succeeded" | "Cancelled" | "Failed";

async function buildRedirectUrl(request: NextRequest, orderId: string) {
  const culture = await getRequestCulture();
  const confirmationPath = buildLocalizedPath(
    `/checkout/orders/${orderId}/confirmation`,
    culture,
  );

  return new URL(confirmationPath, request.url);
}

function applyRedirectParams(
  redirectUrl: URL,
  params: Record<string, string | undefined>,
) {
  const pathWithQuery = buildAppQueryPath(redirectUrl.pathname, params);
  const [pathname, search = ""] = pathWithQuery.split("?");
  redirectUrl.pathname = pathname;
  redirectUrl.search = search ? `?${search}` : "";
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

function readCallbackOrderNumber(searchParams: URLSearchParams) {
  return readSearchTextParam(searchParams.get("orderNumber") ?? undefined, 64);
}

function readProviderReference(searchParams: URLSearchParams) {
  return readSearchTextParam(
    searchParams.get("providerReference") ?? undefined,
    128,
  );
}

function readFailureReason(
  searchParams: URLSearchParams,
  outcome: FinalizeOutcome,
) {
  return (
    readSearchTextParam(searchParams.get("failureReason") ?? undefined, 240) ||
    (outcome === "Cancelled"
      ? "Shopper cancelled hosted checkout."
      : undefined)
  );
}

export async function GET(request: NextRequest, context: FinalizeRouteContext) {
  const { orderId } = await context.params;
  const redirectUrl = await buildRedirectUrl(request, orderId);
  const searchParams = request.nextUrl.searchParams;
  const orderNumber = readCallbackOrderNumber(searchParams);
  const handoff = await readStorefrontPaymentHandoff();

  if (!handoff || handoff.orderId !== orderId || !handoff.paymentId) {
    if (handoff && handoff.orderId !== orderId) {
      await clearStorefrontPaymentHandoff();
    }

    applyRedirectParams(redirectUrl, {
      paymentCompletionStatus: "missing-context",
      orderNumber,
      cancelled: searchParams.get("cancelled") === "true" ? "true" : undefined,
    });
    return NextResponse.redirect(redirectUrl);
  }

  const outcome = resolveOutcome(searchParams);
  const failureReason = readFailureReason(searchParams, outcome);
  const result = await completePublicStorefrontPayment({
    orderId,
    paymentId: handoff.paymentId,
    orderNumber: orderNumber ?? handoff.orderNumber,
    providerReference: readProviderReference(searchParams) || handoff.providerReference,
    outcome,
    failureReason,
  });

  await clearStorefrontPaymentHandoff();

  const redirectBaseParams = {
    orderNumber: orderNumber ?? handoff.orderNumber,
    cancelled: outcome === "Cancelled" ? "true" : undefined,
  };

  if (result.status !== "ok" || !result.data) {
    applyRedirectParams(redirectUrl, {
      ...redirectBaseParams,
      paymentCompletionStatus: "failed",
      paymentError:
        result.message ??
        toLocalizedQueryMessage("checkoutPaymentCompletionFailedMessage"),
    });
    return NextResponse.redirect(redirectUrl);
  }

  applyRedirectParams(redirectUrl, {
    ...redirectBaseParams,
    paymentCompletionStatus: "completed",
    paymentOutcome: outcome,
    paymentStatus: result.data.paymentStatus,
    orderStatus: result.data.orderStatus,
  });

  return NextResponse.redirect(redirectUrl);
}
