import { CartPage } from "@/components/cart/cart-page";
import { getPublicProducts } from "@/features/catalog/api/public-catalog";
import type { PublicProductSummary } from "@/features/catalog/types";
import { getCartViewModel } from "@/features/cart/server/get-cart-view-model";
import {
  readAllowedSearchParam,
  readSingleSearchParam,
} from "@/features/checkout/helpers";
import { getCommerceResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { buildNoIndexMetadata } from "@/lib/seo";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getCommerceResource(culture);

  return buildNoIndexMetadata(
    culture,
    copy.cartMetaTitle,
    copy.cartMetaDescription,
    "/cart",
  );
}

type CartRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function CartRoute({ searchParams }: CartRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const culture = await getRequestCulture();
  const model = await getCartViewModel();
  let followUpProducts: PublicProductSummary[] = [];

  if (model.cart?.items.length) {
    const productResult = await getPublicProducts({
      page: 1,
      pageSize: 6,
      culture,
    });
    const activeHrefs = new Set(
      model.cart.items
        .map((item) => item.display?.href)
        .filter((value): value is string => Boolean(value)),
    );

    followUpProducts = (productResult.data?.items ?? []).filter(
      (product) => !activeHrefs.has(`/catalog/${product.slug}`),
    ).slice(0, 3);
  }

  return (
    <CartPage
      culture={culture}
      model={model}
      cartStatus={readAllowedSearchParam(resolvedSearchParams?.cartStatus, [
        "added",
        "updated",
        "removed",
        "coupon-applied",
        "coupon-cleared",
      ])}
      cartError={readSingleSearchParam(resolvedSearchParams?.cartError)}
      followUpProducts={followUpProducts}
    />
  );
}
