import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getPublicAuthStorefrontContext } from "@/features/account/server/get-public-auth-storefront-context";
import { getPublicCategories, getPublicProducts } from "@/features/catalog/api/public-catalog";
import { readCartDisplaySnapshots } from "@/features/cart/cookies";
import { readPositiveIntegerSearchParam } from "@/features/checkout/helpers";
import { OrdersPage } from "@/components/member/orders-page";
import { getPublishedPages } from "@/features/cms/api/public-cms";
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

  const [ordersResult, cmsPagesResult, categoriesResult, productsResult] = await Promise.all([
    getCurrentMemberOrders({
      page: safePage,
      pageSize: 12,
    }),
    getPublishedPages({ page: 1, pageSize: 2, culture }),
    getPublicCategories(culture),
    getPublicProducts({ page: 1, pageSize: 3, culture }),
  ]);
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
      cmsPages={cmsPagesResult.data?.items ?? []}
      cmsPagesStatus={cmsPagesResult.status}
      categories={categoriesResult.data?.items.slice(0, 3) ?? []}
      categoriesStatus={categoriesResult.status}
      products={productsResult.data?.items ?? []}
      productsStatus={productsResult.status}
      cartLinkedProductSlugs={cartLinkedProductSlugs}
    />
  );
}
