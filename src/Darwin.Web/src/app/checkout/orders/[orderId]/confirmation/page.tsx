import { OrderConfirmationPage } from "@/components/checkout/order-confirmation-page";
import { getPublicStorefrontOrderConfirmation } from "@/features/checkout/api/public-checkout";
import { readSingleSearchParam } from "@/features/checkout/helpers";

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
  const resolvedParams = await params;
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const orderNumber = readSingleSearchParam(resolvedSearchParams?.orderNumber);
  const checkoutStatus = readSingleSearchParam(resolvedSearchParams?.checkoutStatus);
  const paymentError = readSingleSearchParam(resolvedSearchParams?.paymentError);
  const cancelled = readSingleSearchParam(resolvedSearchParams?.cancelled) === "true";
  const confirmationResult = await getPublicStorefrontOrderConfirmation(
    resolvedParams.orderId,
    orderNumber,
  );

  return (
    <OrderConfirmationPage
      confirmation={confirmationResult.data}
      status={confirmationResult.status}
      message={confirmationResult.message}
      checkoutStatus={checkoutStatus}
      paymentError={paymentError}
      cancelled={cancelled}
    />
  );
}
