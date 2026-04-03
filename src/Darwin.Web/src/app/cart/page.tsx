import { CartPage } from "@/components/cart/cart-page";
import { getPublicProducts } from "@/features/catalog/api/public-catalog";
import type { PublicProductSummary } from "@/features/catalog/types";
import { getCartViewModel } from "@/features/cart/server/get-cart-view-model";
import {
  readAllowedSearchParam,
  readSingleSearchParam,
} from "@/features/checkout/helpers";
import { getMemberSession } from "@/features/member-session/cookies";
import { getMemberIdentityContext } from "@/features/member-portal/server/get-member-summary-context";
import { getStorefrontContinuationContext } from "@/features/storefront/server/get-storefront-continuation-context";
import { getCommerceResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { observeAsyncOperation } from "@/lib/route-observability";
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
  const [model, memberSession, storefrontContext] = await observeAsyncOperation(
    {
      area: "cart",
      operation: "load-route",
      thresholdMs: 300,
    },
    () =>
      Promise.all([
        getCartViewModel(),
        getMemberSession(),
        getStorefrontContinuationContext(culture),
      ]),
  );
  const identityContext = memberSession ? await getMemberIdentityContext() : null;
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
      cmsPages={storefrontContext.cmsPages}
      cmsPagesStatus={storefrontContext.cmsPagesStatus}
      categories={storefrontContext.categories}
      categoriesStatus={storefrontContext.categoriesStatus}
    />
  );
}
