import { PasswordPage } from "@/components/account/password-page";
import { getPublicAuthStorefrontContext } from "@/features/account/server/get-public-auth-storefront-context";
import { sanitizeAppPath } from "@/lib/locale-routing";
import { getRequestCulture } from "@/lib/request-culture";
import { buildNoIndexMetadata } from "@/lib/seo";
import { getMemberResource } from "@/localization";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);

  return buildNoIndexMetadata(
    culture,
    copy.passwordMetaTitle,
    undefined,
    "/account/password",
  );
}

type PasswordRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(
  value: string | string[] | undefined,
) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function PasswordRoute({ searchParams }: PasswordRouteProps) {
  const culture = await getRequestCulture();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const storefrontContext = await getPublicAuthStorefrontContext(culture);

  return (
    <PasswordPage
      culture={culture}
      email={readSearchParam(resolvedSearchParams?.email)}
      token={readSearchParam(resolvedSearchParams?.token)}
      passwordStatus={readSearchParam(resolvedSearchParams?.passwordStatus)}
      passwordError={readSearchParam(resolvedSearchParams?.passwordError)}
      returnPath={sanitizeAppPath(
        readSearchParam(resolvedSearchParams?.returnPath),
        "/account",
      )}
      cmsPages={storefrontContext.cmsPages}
      cmsPagesStatus={storefrontContext.cmsPagesStatus}
      categories={storefrontContext.categories}
      categoriesStatus={storefrontContext.categoriesStatus}
      storefrontCart={storefrontContext.storefrontCart}
      storefrontCartStatus={storefrontContext.storefrontCartStatus}
    />
  );
}
