import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { OrdersPage } from "@/components/member/orders-page";
import { getCurrentMemberOrders } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { buildNoIndexMetadata } from "@/lib/seo";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);

  return buildNoIndexMetadata(culture, copy.ordersMetaTitle, undefined, "/orders");
}

type OrdersRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function OrdersRoute({ searchParams }: OrdersRouteProps) {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);
  const session = await getMemberSession();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const page = Number((Array.isArray(resolvedSearchParams?.page) ? resolvedSearchParams?.page[0] : resolvedSearchParams?.page) ?? "1");
  const safePage = Number.isFinite(page) && page > 0 ? page : 1;

  if (!session) {
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.ordersAuthRequiredTitle}
        message={copy.ordersAuthRequiredMessage}
        returnPath="/orders"
      />
    );
  }

  const ordersResult = await getCurrentMemberOrders({
    page: safePage,
    pageSize: 12,
  });

  return (
    <OrdersPage
      culture={culture}
      orders={ordersResult.data?.items ?? []}
      status={ordersResult.status}
      currentPage={safePage}
      totalPages={Math.max(1, Math.ceil((ordersResult.data?.total ?? 0) / (ordersResult.data?.request.pageSize ?? 12)))}
    />
  );
}
