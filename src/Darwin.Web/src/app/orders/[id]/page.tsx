import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { OrderDetailPage } from "@/components/member/order-detail-page";
import { getMemberOrderDetailPageContext } from "@/features/member-portal/server/get-member-protected-page-context";
import { getOrderDetailSeoMetadata } from "@/features/member-portal/server/get-member-route-seo-metadata";
import { getMemberResource } from "@/localization";
import { buildOrderPath } from "@/lib/entity-paths";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata({ params }: OrderDetailRouteProps) {
  const culture = await getRequestCulture();
  const { id } = await params;
  const { metadata } = await getOrderDetailSeoMetadata(culture, id);
  return metadata;
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
  const { id } = await params;
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const { entryContext, routeContext } = await getMemberOrderDetailPageContext(
    culture,
    id,
  );
  const { session, storefrontContext: authStorefrontContext } = entryContext;

  if (!session) {
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.orderDetailAuthRequiredTitle}
        message={copy.orderDetailAuthRequiredMessage}
        returnPath={buildOrderPath(id)}
        cmsPages={authStorefrontContext!.cmsPages}
        cmsPagesStatus={authStorefrontContext!.cmsPagesStatus}
        categories={authStorefrontContext!.categories}
        categoriesStatus={authStorefrontContext!.categoriesStatus}
        products={authStorefrontContext!.products}
        productsStatus={authStorefrontContext!.productsStatus}
        storefrontCart={authStorefrontContext!.storefrontCart}
        storefrontCartStatus={authStorefrontContext!.storefrontCartStatus}
      />
    );
  }

  const { orderResult, storefrontContext } = routeContext!;

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
      cartLinkedProductSlugs={storefrontContext.cartLinkedProductSlugs}
      storefrontCart={storefrontContext.storefrontCart}
      storefrontCartStatus={storefrontContext.storefrontCartStatus}
    />
  );
}
