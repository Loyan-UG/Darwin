import { redirect } from "next/navigation";
import { OrderConfirmationPage } from "@/components/checkout/order-confirmation-page";
import { getPublicStorefrontOrderConfirmation } from "@/features/checkout/api/public-checkout";
import { readStorefrontPaymentHandoff } from "@/features/checkout/cookies";
import {
  getCurrentMemberInvoices,
  getCurrentMemberLoyaltyOverview,
  getCurrentMemberOrders,
} from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getStorefrontContinuationContext } from "@/features/storefront/server/get-storefront-continuation-context";
import {
  readAllowedSearchParam,
  readSingleSearchParam,
} from "@/features/checkout/helpers";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { getCommerceResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { observeAsyncOperation } from "@/lib/route-observability";
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

  const [confirmationResult, memberSession, storefrontContext] =
    await observeAsyncOperation(
      {
        area: "confirmation",
        operation: "load-route",
        thresholdMs: 350,
      },
      () =>
        Promise.all([
          getPublicStorefrontOrderConfirmation(
            resolvedParams.orderId,
            orderNumber,
          ),
          getMemberSession(),
          getStorefrontContinuationContext(culture),
        ]),
    );
  const [memberOrdersResult, memberInvoicesResult, memberLoyaltyOverviewResult] =
    memberSession
      ? await Promise.all([
          getCurrentMemberOrders({
            page: 1,
            pageSize: 2,
          }),
          getCurrentMemberInvoices({
            page: 1,
            pageSize: 2,
          }),
          getCurrentMemberLoyaltyOverview(),
        ])
      : [null, null, null];
  const purchasedNames = new Set(
    (confirmationResult.data?.lines ?? []).map((line) => line.name.trim().toLowerCase()),
  );
  const followUpProducts = storefrontContext.products.filter(
    (product) => !purchasedNames.has(product.name.trim().toLowerCase()),
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
      paymentError={paymentError}
      cancelled={cancelled}
      hasMemberSession={Boolean(memberSession)}
      memberOrders={memberOrdersResult?.data?.items ?? []}
      memberOrdersStatus={memberOrdersResult?.status ?? "idle"}
      memberInvoices={memberInvoicesResult?.data?.items ?? []}
      memberInvoicesStatus={memberInvoicesResult?.status ?? "idle"}
      memberLoyaltyOverview={memberLoyaltyOverviewResult?.data ?? null}
      memberLoyaltyStatus={memberLoyaltyOverviewResult?.status ?? "idle"}
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
