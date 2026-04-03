import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getPublicAuthStorefrontContext } from "@/features/account/server/get-public-auth-storefront-context";
import { readCartDisplaySnapshots } from "@/features/cart/cookies";
import { readPositiveIntegerSearchParam } from "@/features/checkout/helpers";
import { OrdersPage } from "@/components/member/orders-page";
import { getCurrentMemberOrders } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getStorefrontContinuationContext } from "@/features/storefront/server/get-storefront-continuation-context";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { observeAsyncOperation } from "@/lib/route-observability";
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
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);

  if (!session) {
    const storefrontContext = await getPublicAuthStorefrontContext(culture);
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.ordersAuthRequiredTitle}
        message={copy.ordersAuthRequiredMessage}
        returnPath="/orders"
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

  const [ordersResult, storefrontContext] = await observeAsyncOperation(
    {
      area: "orders",
      operation: "load-route",
      thresholdMs: 325,
    },
    () =>
      Promise.all([
        getCurrentMemberOrders({
          page: safePage,
          pageSize: 12,
        }),
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
    <OrdersPage
      culture={culture}
      orders={ordersResult.data?.items ?? []}
      status={ordersResult.status}
      currentPage={safePage}
      totalPages={Math.max(1, Math.ceil((ordersResult.data?.total ?? 0) / (ordersResult.data?.request.pageSize ?? 12)))}
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
