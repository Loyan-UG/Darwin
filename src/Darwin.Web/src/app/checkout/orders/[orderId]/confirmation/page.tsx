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
        `/checkout/orders/${resolvedParams.orderId}/confirmation/finalize`,
        {
          orderNumber,
          cancelled: cancelled ? "true" : undefined,
        },
      ),
    );
  }

  const { routeContext, followUpProducts } = await getConfirmationPageContext(
    culture,
    resolvedParams.orderId,
    orderNumber,
  );
  const {
    confirmationResult,
    memberSession,
    commerceSummaryContext,
    storefrontContext,
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
      memberOrders={commerceSummaryContext?.ordersResult.data?.items.slice(0, 2) ?? []}
      memberOrdersStatus={commerceSummaryContext?.ordersResult.status ?? "idle"}
      memberInvoices={commerceSummaryContext?.invoicesResult.data?.items.slice(0, 2) ?? []}
      memberInvoicesStatus={commerceSummaryContext?.invoicesResult.status ?? "idle"}
      memberLoyaltyOverview={commerceSummaryContext?.loyaltyOverviewResult.data ?? null}
      memberLoyaltyStatus={commerceSummaryContext?.loyaltyOverviewResult.status ?? "idle"}
      cmsPages={storefrontContext.cmsPages}
      cmsPagesStatus={storefrontContext.cmsPagesStatus}
      categories={storefrontContext.categories}
      categoriesStatus={storefrontContext.categoriesStatus}
      products={
        followUpProducts.length > 0 ? followUpProducts : storefrontContext.products
      }
      productsStatus={storefrontContext.productsStatus}
    />
  );
}
