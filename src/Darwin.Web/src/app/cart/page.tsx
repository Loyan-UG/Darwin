import { CartPage } from "@/components/cart/cart-page";
import { getCartViewModel } from "@/features/cart/server/get-cart-view-model";
import { getRequestCulture } from "@/lib/request-culture";

export const metadata = {
  title: "Cart",
};

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
