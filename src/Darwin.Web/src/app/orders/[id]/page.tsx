import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getPublicAuthStorefrontContext } from "@/features/account/server/get-public-auth-storefront-context";
import { readCartDisplaySnapshots } from "@/features/cart/cookies";
import { OrderDetailPage } from "@/components/member/order-detail-page";
import { getCurrentMemberOrder } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getStorefrontContinuationContext } from "@/features/storefront/server/get-storefront-continuation-context";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { observeAsyncOperation } from "@/lib/route-observability";
import { buildNoIndexMetadata } from "@/lib/seo";

export async function generateMetadata({ params }: OrderDetailRouteProps) {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);
  const { id } = await params;

  return buildNoIndexMetadata(
    culture,
    copy.orderDetailMetaTitle,
    undefined,
    `/orders/${id}`,
  );
}

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
  const copy = getMemberResource(culture);
  const session = await getMemberSession();
  const { id } = await params;
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  if (!session) {
    const storefrontContext = await getPublicAuthStorefrontContext(culture);
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.orderDetailAuthRequiredTitle}
        message={copy.orderDetailAuthRequiredMessage}
        returnPath={`/orders/${id}`}
        cmsPages={storefrontContext.cmsPages}
        cmsPagesStatus={storefrontContext.cmsPagesStatus}
        categories={storefrontContext.categories}
        categoriesStatus={storefrontContext.categoriesStatus}
        products={storefrontContext.products}
        productsStatus={storefrontContext.productsStatus}
        storefrontCart={storefrontContext.storefrontCart}
        storefrontCartStatus={storefrontContext.storefrontCartStatus}
      />
    );
  }

  const [orderResult, storefrontContext] = await observeAsyncOperation(
    {
      area: "order-detail",
      operation: "load-route",
      thresholdMs: 325,
    },
    () =>
      Promise.all([
        getCurrentMemberOrder(id),
        getStorefrontContinuationContext(culture),
      ]),
  );
  const cartLinkedProductSlugs = (await readCartDisplaySnapshots())
    .map((snapshot) => {
      const match = snapshot.href.match(/\/catalog\/([^/?#]+)/i);
      return match?.[1] ?? null;
    })
    .filter((slug): slug is string => Boolean(slug));

  return (
    <OrderDetailPage
      culture={culture}
      order={orderResult.data}
      status={orderResult.status}
      paymentError={readSearchParam(resolvedSearchParams?.paymentError)}
      cmsPages={storefrontContext.cmsPages}
      cmsPagesStatus={storefrontContext.cmsPagesStatus}
      categories={storefrontContext.categories}
      categoriesStatus={storefrontContext.categoriesStatus}
      products={storefrontContext.products}
      productsStatus={storefrontContext.productsStatus}
      cartLinkedProductSlugs={cartLinkedProductSlugs}
    />
  );
}
