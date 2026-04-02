import { CheckoutPage } from "@/components/checkout/checkout-page";
import { getCartViewModel } from "@/features/cart/server/get-cart-view-model";
import { createPublicCheckoutIntent } from "@/features/checkout/api/public-checkout";
import {
  isCheckoutAddressComplete,
  readCheckoutDraftFromSearchParams,
  readSingleSearchParam,
  toCheckoutAddress,
} from "@/features/checkout/helpers";
import { getCommerceResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { buildNoIndexMetadata } from "@/lib/seo";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getCommerceResource(culture);

  return buildNoIndexMetadata(
    culture,
    copy.checkoutMetaTitle,
    copy.checkoutMetaDescription,
    "/checkout",
  );
}

type CheckoutRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function CheckoutRoute({ searchParams }: CheckoutRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const culture = await getRequestCulture();
  const model = await getCartViewModel();
  const draft = readCheckoutDraftFromSearchParams(resolvedSearchParams);
  const checkoutError = readSingleSearchParam(resolvedSearchParams?.checkoutError);

  let intent = null;
  let intentStatus = "idle";
  let intentMessage: string | undefined;

  if (model.cart && isCheckoutAddressComplete(draft)) {
    const intentResult = await createPublicCheckoutIntent({
      cartId: model.cart.cartId,
      shippingAddress: toCheckoutAddress(draft),
      selectedShippingMethodId: draft.selectedShippingMethodId || undefined,
    });

    intent = intentResult.data;
    intentStatus = intentResult.status;
    intentMessage = intentResult.message;
  }

  return (
    <CheckoutPage
      culture={culture}
      model={model}
      draft={draft}
      intent={intent}
      intentStatus={intentStatus}
      intentMessage={intentMessage}
      checkoutError={checkoutError}
    />
  );
}
