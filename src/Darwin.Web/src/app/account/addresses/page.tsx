import { AddressesPage } from "@/components/account/addresses-page";
import { getPublicAuthStorefrontContext } from "@/features/account/server/get-public-auth-storefront-context";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getCurrentMemberAddresses } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getStorefrontContinuationContext } from "@/features/storefront/server/get-storefront-continuation-context";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { observeAsyncOperation } from "@/lib/route-observability";
import { buildNoIndexMetadata } from "@/lib/seo";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);

  return buildNoIndexMetadata(
    culture,
    copy.addressesMetaTitle,
    undefined,
    "/account/addresses",
  );
}

type AddressesRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function AddressesRoute({
  searchParams,
}: AddressesRouteProps) {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);
  const session = await getMemberSession();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  if (!session) {
    const storefrontContext = await getPublicAuthStorefrontContext(culture);
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.addressesAuthRequiredTitle}
        message={copy.addressesAuthRequiredMessage}
        returnPath="/account/addresses"
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

  const [addressesResult, storefrontContext] = await observeAsyncOperation(
    {
      area: "addresses",
      operation: "load-route",
      thresholdMs: 300,
    },
    () =>
      Promise.all([
        getCurrentMemberAddresses(),
        getStorefrontContinuationContext(culture),
      ]),
  );

  return (
    <AddressesPage
      culture={culture}
      addresses={addressesResult.data ?? []}
      status={addressesResult.status}
      addressesStatus={readSearchParam(resolvedSearchParams?.addressesStatus)}
      addressesError={readSearchParam(resolvedSearchParams?.addressesError)}
      cmsPages={storefrontContext.cmsPages}
      cmsPagesStatus={storefrontContext.cmsPagesStatus}
      categories={storefrontContext.categories}
      categoriesStatus={storefrontContext.categoriesStatus}
      products={storefrontContext.products}
      productsStatus={storefrontContext.productsStatus}
    />
  );
}
