import { RegisterPage } from "@/components/account/register-page";
import { getPublicRegisterPageContext } from "@/features/account/server/get-public-auth-page-context";
import { getPublicRegisterSeoMetadata } from "@/features/account/server/get-public-auth-seo-metadata";
import { sanitizeAppPath } from "@/lib/locale-routing";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getPublicRegisterSeoMetadata(culture);
  return metadata;
}

type RegisterRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(
  value: string | string[] | undefined,
) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function RegisterRoute({ searchParams }: RegisterRouteProps) {
  const culture = await getRequestCulture();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const { storefrontProps } = await getPublicRegisterPageContext(culture);

  return (
    <RegisterPage
      culture={culture}
      email={readSearchParam(resolvedSearchParams?.email)}
      registerStatus={readSearchParam(resolvedSearchParams?.registerStatus)}
      registerError={readSearchParam(resolvedSearchParams?.registerError)}
      returnPath={sanitizeAppPath(
        readSearchParam(resolvedSearchParams?.returnPath),
        "/account",
      )}
      {...storefrontProps}
    />
  );
}
