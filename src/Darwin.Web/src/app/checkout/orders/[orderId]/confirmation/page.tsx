import { redirect } from "next/navigation";
import { OrderConfirmationPage } from "@/components/checkout/order-confirmation-page";
import { getPublicStorefrontOrderConfirmation } from "@/features/checkout/api/public-checkout";
import { readStorefrontPaymentHandoff } from "@/features/checkout/cookies";
import { readSingleSearchParam } from "@/features/checkout/helpers";
import { getRequestCulture } from "@/lib/request-culture";

export const metadata = {
  title: "Order confirmation",
};

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
  const checkoutStatus = readSingleSearchParam(resolvedSearchParams?.checkoutStatus);
  const paymentCompletionStatus = readSingleSearchParam(
    resolvedSearchParams?.paymentCompletionStatus,
  );
  const paymentOutcome = readSingleSearchParam(resolvedSearchParams?.paymentOutcome);
  const paymentStatus = readSingleSearchParam(resolvedSearchParams?.paymentStatus);
  const orderStatus = readSingleSearchParam(resolvedSearchParams?.orderStatus);
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

  return (
    <OrderConfirmationPage
      culture={culture}
      confirmation={confirmationResult.data}
      status={confirmationResult.status}
      message={confirmationResult.message}
      checkoutStatus={checkoutStatus}
      paymentCompletionStatus={paymentCompletionStatus}
      paymentOutcome={paymentOutcome}
      paymentStatus={paymentStatus}
      orderStatus={orderStatus}
      paymentError={paymentError}
      cancelled={cancelled}
    />
  );
}
