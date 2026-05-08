import { CartPage } from "@/components/cart/cart-page";
import { getCartSeoMetadata } from "@/features/checkout/server/get-commerce-seo-metadata";
import { getCartPageContext } from "@/features/checkout/server/get-commerce-page-context";
import {
  readAllowedSearchParam,
  readSingleSearchParam,
} from "@/features/checkout/helpers";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getCartSeoMetadata(culture);
  return metadata;
}

type CartRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function CartRoute({ searchParams }: CartRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const culture = await getRequestCulture();
  const { routeContext, followUpProducts } = await getCartPageContext(culture);
  const { model, memberSession, identityContext } = routeContext;

  return (
    <CartPage
      culture={culture}
      model={model}
      memberAddresses={identityContext?.addressesResult.data ?? []}
      memberAddressesStatus={identityContext?.addressesResult.status ?? "unauthenticated"}
      memberProfile={identityContext?.profileResult.data ?? null}
      memberProfileStatus={identityContext?.profileResult.status ?? "unauthenticated"}
      memberPreferences={identityContext?.preferencesResult.data ?? null}
      memberPreferencesStatus={identityContext?.preferencesResult.status ?? "unauthenticated"}
      hasMemberSession={Boolean(memberSession)}
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
