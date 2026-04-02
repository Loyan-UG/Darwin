import { CartPage } from "@/components/cart/cart-page";
import { getCartViewModel } from "@/features/cart/server/get-cart-view-model";
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
  searchParams?: Promise<{
    cartStatus?: string;
    cartError?: string;
  }>;
};

export default async function CartRoute({ searchParams }: CartRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const culture = await getRequestCulture();
  const model = await getCartViewModel();

  return (
    <CartPage
      culture={culture}
      model={model}
      cartStatus={resolvedSearchParams?.cartStatus}
      cartError={resolvedSearchParams?.cartError}
    />
  );
}
