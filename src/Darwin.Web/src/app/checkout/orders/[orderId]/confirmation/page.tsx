import { redirect } from "next/navigation";
import { OrderConfirmationPage } from "@/components/checkout/order-confirmation-page";
import { getConfirmationPageContext } from "@/features/checkout/server/get-commerce-page-context";
import { getConfirmationSeoMetadata } from "@/features/checkout/server/get-commerce-seo-metadata";
import { readStorefrontPaymentHandoff } from "@/features/checkout/cookies";
import {
  readAllowedSearchParam,
  readSingleSearchParam,
} from "@/features/checkout/helpers";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata({ params }: ConfirmationRouteProps) {
  const culture = await getRequestCulture();
  const { orderId } = await params;
  const { metadata } = await getConfirmationSeoMetadata(culture, orderId);
  return metadata;
}

type ConfirmationRouteProps = {
  params: Promise<{
    orderId: string;
  }>;
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function buildFinalizePath(orderId: string) {
  return `/checkout/orders/${encodeURIComponent(orderId)}/confirmation/finalize`;
}

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
    redirect(
      buildAppQueryPath(
        buildFinalizePath(resolvedParams.orderId),
        {
          orderNumber,
          cancelled: cancelled ? "true" : undefined,
        },
      ),
    );
  }

  const { routeContext } = await getConfirmationPageContext(
    culture,
    resolvedParams.orderId,
    orderNumber,
  );
  const {
    confirmationResult,
    memberSession,
  } = routeContext;

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
