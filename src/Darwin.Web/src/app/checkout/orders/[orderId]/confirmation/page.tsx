import { redirect } from "next/navigation";
import { getPublicCategories, getPublicProducts } from "@/features/catalog/api/public-catalog";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import { OrderConfirmationPage } from "@/components/checkout/order-confirmation-page";
import { getPublicStorefrontOrderConfirmation } from "@/features/checkout/api/public-checkout";
import { readStorefrontPaymentHandoff } from "@/features/checkout/cookies";
import {
  getCurrentMemberInvoices,
  getCurrentMemberLoyaltyOverview,
  getCurrentMemberOrders,
} from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import {
  readAllowedSearchParam,
  readSingleSearchParam,
} from "@/features/checkout/helpers";
import { buildAppQueryPath } from "@/lib/locale-routing";
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

  const [confirmationResult, memberSession, cmsPagesResult, categoriesResult, productsResult] = await Promise.all([
    getPublicStorefrontOrderConfirmation(
      resolvedParams.orderId,
      orderNumber,
    ),
    getMemberSession(),
    getPublishedPages({
      page: 1,
      pageSize: 3,
      culture,
    }),
    getPublicCategories(culture),
    getPublicProducts({
      page: 1,
      pageSize: 3,
      culture,
    }),
  ]);
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
      cmsPages={cmsPagesResult.data?.items ?? []}
      cmsPagesStatus={cmsPagesResult.status}
      categories={categoriesResult.data?.items.slice(0, 3) ?? []}
      categoriesStatus={categoriesResult.status}
      products={productsResult.data?.items ?? []}
      productsStatus={productsResult.status}
    />
  );
}
