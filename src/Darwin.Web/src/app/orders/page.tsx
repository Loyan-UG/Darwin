import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { OrdersPage } from "@/components/member/orders-page";
import { getCurrentMemberOrders } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getRequestCulture } from "@/lib/request-culture";

export const metadata = {
  title: "Orders",
};

type OrdersRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function OrdersRoute({ searchParams }: OrdersRouteProps) {
  const culture = await getRequestCulture();
  const session = await getMemberSession();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const page = Number((Array.isArray(resolvedSearchParams?.page) ? resolvedSearchParams?.page[0] : resolvedSearchParams?.page) ?? "1");
  const safePage = Number.isFinite(page) && page > 0 ? page : 1;

  if (!session) {
    return (
      <MemberAuthRequired
        title="Member sign-in is required for order history."
        message="Orders now live behind the authenticated member portal and no longer use a storefront placeholder route."
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
