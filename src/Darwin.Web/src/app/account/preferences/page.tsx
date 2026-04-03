import { PreferencesPage } from "@/components/account/preferences-page";
import { getPublicAuthStorefrontContext } from "@/features/account/server/get-public-auth-storefront-context";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import {
  getCurrentMemberPreferences,
  getCurrentMemberProfile,
} from "@/features/member-portal/api/member-portal";
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
    copy.preferencesMetaTitle,
    undefined,
    "/account/preferences",
  );
}

type PreferencesRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function PreferencesRoute({
  searchParams,
}: PreferencesRouteProps) {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);
  const session = await getMemberSession();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  if (!session) {
    const storefrontContext = await getPublicAuthStorefrontContext(culture);
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.preferencesAuthRequiredTitle}
        message={copy.preferencesAuthRequiredMessage}
        returnPath="/account/preferences"
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

  const [preferencesResult, profileResult, storefrontContext] =
    await observeAsyncOperation(
      {
        area: "preferences",
        operation: "load-route",
        thresholdMs: 300,
      },
      () =>
        Promise.all([
          getCurrentMemberPreferences(),
          getCurrentMemberProfile(),
          getStorefrontContinuationContext(culture),
        ]),
    );

  return (
    <PreferencesPage
      culture={culture}
      preferences={preferencesResult.data}
      status={preferencesResult.status}
      profile={profileResult.data}
      profileStatus={profileResult.status}
      preferencesStatus={readSearchParam(resolvedSearchParams?.preferencesStatus)}
      preferencesError={readSearchParam(resolvedSearchParams?.preferencesError)}
      cmsPages={storefrontContext.cmsPages}
      cmsPagesStatus={storefrontContext.cmsPagesStatus}
      categories={storefrontContext.categories}
      categoriesStatus={storefrontContext.categoriesStatus}
      products={storefrontContext.products}
      productsStatus={storefrontContext.productsStatus}
    />
  );
}
