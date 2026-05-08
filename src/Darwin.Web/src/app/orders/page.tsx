import { MemberAuthRequired } from "@/components/member/member-auth-required";
import {
  readAllowedSearchParam,
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
import { OrdersPage } from "@/components/member/orders-page";
import { getMemberOrdersPageContext } from "@/features/member-portal/server/get-member-protected-page-context";
import { getOrdersSeoMetadata } from "@/features/member-portal/server/get-member-route-seo-metadata";
import { createStorefrontCartProps } from "@/features/storefront/route-projections";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getOrdersSeoMetadata(culture);
  return metadata;
}

type OrdersRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function OrdersRoute({ searchParams }: OrdersRouteProps) {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);
  const visibleQuery = readSearchTextParam(resolvedSearchParams?.visibleQuery);
  const visibleState = readAllowedSearchParam(
    resolvedSearchParams?.visibleState,
    ["all", "attention", "settled"] as const,
  );
  const { entryContext, routeContext } = await getMemberOrdersPageContext(
    culture,
    safePage,
    12,
  );
  const { session, storefrontContext: authStorefrontContext } = entryContext;

  if (!session) {
    const storefrontProps = createStorefrontCartProps(authStorefrontContext!);
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.ordersAuthRequiredTitle}
        message={copy.ordersAuthRequiredMessage}
        returnPath="/orders"
        {...storefrontProps}
      />
    );
  }

  const { ordersResult } = routeContext!;

  return (
    <OrdersPage
      culture={culture}
      orders={ordersResult.data?.items ?? []}
      status={ordersResult.status}
      currentPage={safePage}
      totalPages={Math.max(1, Math.ceil((ordersResult.data?.total ?? 0) / (ordersResult.data?.request.pageSize ?? 12)))}
      visibleQuery={visibleQuery}
      visibleState={visibleState ?? "all"}
    />
  );
}
