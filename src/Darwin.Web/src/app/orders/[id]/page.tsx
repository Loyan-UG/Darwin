import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { OrderDetailPage } from "@/components/member/order-detail-page";
import { getCurrentMemberOrder } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getRequestCulture } from "@/lib/request-culture";

export const metadata = {
  title: "Order detail",
};

type OrderDetailRouteProps = {
  params: Promise<{
    id: string;
  }>;
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function OrderDetailRoute({
  params,
  searchParams,
}: OrderDetailRouteProps) {
  const culture = await getRequestCulture();
  const session = await getMemberSession();
  const { id } = await params;
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  if (!session) {
    return (
      <MemberAuthRequired
        title="Member sign-in is required for order detail."
        message="Order detail is now an authenticated member route."
        returnPath={`/orders/${id}`}
      />
    );
  }

  const orderResult = await getCurrentMemberOrder(id);

  return (
    <OrderDetailPage
      culture={culture}
      order={orderResult.data}
      status={orderResult.status}
      paymentError={readSearchParam(resolvedSearchParams?.paymentError)}
    />
  );
}
