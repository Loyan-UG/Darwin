import { CartPage } from "@/components/cart/cart-page";
import { getCartViewModel } from "@/features/cart/server/get-cart-view-model";

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
  const model = await getCartViewModel();

  return (
    <CartPage
      model={model}
      cartStatus={resolvedSearchParams?.cartStatus}
      cartError={resolvedSearchParams?.cartError}
    />
  );
}
