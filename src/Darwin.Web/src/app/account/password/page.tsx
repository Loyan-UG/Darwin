import { PasswordPage } from "@/components/account/password-page";
import { getPublicPasswordPageContext } from "@/features/account/server/get-public-auth-page-context";
import { getPublicPasswordSeoMetadata } from "@/features/account/server/get-public-auth-seo-metadata";
import { sanitizeAppPath } from "@/lib/locale-routing";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getPublicPasswordSeoMetadata(culture);
  return metadata;
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
  const { storefrontProps } = await getPublicPasswordPageContext(culture);

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
      {...storefrontProps}
    />
  );
}
