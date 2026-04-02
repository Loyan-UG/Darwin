import { redirect } from "next/navigation";
import { OrderConfirmationPage } from "@/components/checkout/order-confirmation-page";
import { getPublicStorefrontOrderConfirmation } from "@/features/checkout/api/public-checkout";
import { readStorefrontPaymentHandoff } from "@/features/checkout/cookies";
import { getMemberSession } from "@/features/member-session/cookies";
import {
  readAllowedSearchParam,
  readSingleSearchParam,
} from "@/features/checkout/helpers";
import { getCommerceResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { buildNoIndexMetadata } from "@/lib/seo";

export async function generateMetadata({ params }: ConfirmationRouteProps) {
  const culture = await getRequestCulture();
  const copy = getCommerceResource(culture);
  const { orderId } = await params;

  return buildNoIndexMetadata(
    culture,
    copy.confirmationMetaTitle,
    copy.confirmationMetaDescription,
    `/checkout/orders/${orderId}/confirmation`,
  );
}

type ConfirmationRouteProps = {
  params: Promise<{
    orderId: string;
  }>;
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function OrderConfirmationRoute({
  params,
  searchParams,
}: ConfirmationRouteProps) {
  const culture = await getRequestCulture();
  const resolvedParams = await params;
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const orderNumber = readSingleSearchParam(resolvedSearchParams?.orderNumber);
  const checkoutStatus = readAllowedSearchParam(
    resolvedSearchParams?.checkoutStatus,
    ["order-placed"],
  );
  const paymentCompletionStatus = readAllowedSearchParam(
    resolvedSearchParams?.paymentCompletionStatus,
    [
      "completed",
      "failed",
      "missing-context",
    ],
  );
  const paymentOutcome = readAllowedSearchParam(
    resolvedSearchParams?.paymentOutcome,
    ["Succeeded", "Cancelled", "Failed"],
  );
  const paymentError = readSingleSearchParam(resolvedSearchParams?.paymentError);
  const cancelled = readSingleSearchParam(resolvedSearchParams?.cancelled) === "true";
  const handoff = await readStorefrontPaymentHandoff();

  if (
    !paymentCompletionStatus &&
    !paymentError &&
    handoff?.orderId === resolvedParams.orderId
  ) {
    const params = new URLSearchParams();
    if (orderNumber) {
      params.set("orderNumber", orderNumber);
    }
    if (cancelled) {
      params.set("cancelled", "true");
    }

    redirect(
      `/checkout/orders/${resolvedParams.orderId}/confirmation/finalize${
        params.size > 0 ? `?${params.toString()}` : ""
      }`,
    );
  }

  const confirmationResult = await getPublicStorefrontOrderConfirmation(
    resolvedParams.orderId,
    orderNumber,
  );
  const memberSession = await getMemberSession();

  return (
    <OrderConfirmationPage
      culture={culture}
      confirmation={confirmationResult.data}
      status={confirmationResult.status}
      message={confirmationResult.message}
      checkoutStatus={checkoutStatus}
      paymentCompletionStatus={paymentCompletionStatus}
      paymentOutcome={paymentOutcome}
      paymentError={paymentError}
      cancelled={cancelled}
      hasMemberSession={Boolean(memberSession)}
    />
  );
}
