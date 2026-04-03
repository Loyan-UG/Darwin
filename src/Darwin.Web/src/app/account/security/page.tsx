import { SecurityPage } from "@/components/account/security-page";
import { getPublicAuthStorefrontContext } from "@/features/account/server/get-public-auth-storefront-context";
import { getPublicCategories, getPublicProducts } from "@/features/catalog/api/public-catalog";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getCurrentMemberProfile } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { buildNoIndexMetadata } from "@/lib/seo";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);

  return buildNoIndexMetadata(
    culture,
    copy.securityMetaTitle,
    undefined,
    "/account/security",
  );
}

type SecurityRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function SecurityRoute({
  searchParams,
}: SecurityRouteProps) {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);
  const session = await getMemberSession();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  if (!session) {
    const storefrontContext = await getPublicAuthStorefrontContext(culture);
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.securityAuthRequiredTitle}
        message={copy.securityAuthRequiredMessage}
        returnPath="/account/security"
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

  const [profileResult, cmsPagesResult, categoriesResult, productsResult] = await Promise.all([
    getCurrentMemberProfile(),
    getPublishedPages({ page: 1, pageSize: 2, culture }),
    getPublicCategories(culture),
    getPublicProducts({ page: 1, pageSize: 3, culture }),
  ]);

  return (
    <SecurityPage
      culture={culture}
      session={session}
      profile={profileResult.data}
      profileStatus={profileResult.status}
      securityStatus={readSearchParam(resolvedSearchParams?.securityStatus)}
      securityError={readSearchParam(resolvedSearchParams?.securityError)}
      cmsPages={cmsPagesResult.data?.items ?? []}
      cmsPagesStatus={cmsPagesResult.status}
      categories={categoriesResult.data?.items.slice(0, 3) ?? []}
      categoriesStatus={categoriesResult.status}
      products={productsResult.data?.items ?? []}
      productsStatus={productsResult.status}
    />
  );
}
