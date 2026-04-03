import { ActivationPage } from "@/components/account/activation-page";
import { getPublicActivationPageContext } from "@/features/account/server/get-public-auth-page-context";
import { getPublicActivationSeoMetadata } from "@/features/account/server/get-public-auth-seo-metadata";
import { sanitizeAppPath } from "@/lib/locale-routing";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getPublicActivationSeoMetadata(culture);
  return metadata;
}

type ActivationRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(
  value: string | string[] | undefined,
) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function ActivationRoute({
  searchParams,
}: ActivationRouteProps) {
  const culture = await getRequestCulture();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const { storefrontProps } = await getPublicActivationPageContext(culture);

  return (
    <ActivationPage
      culture={culture}
      email={readSearchParam(resolvedSearchParams?.email)}
      token={readSearchParam(resolvedSearchParams?.token)}
      activationStatus={readSearchParam(resolvedSearchParams?.activationStatus)}
      activationError={readSearchParam(resolvedSearchParams?.activationError)}
      returnPath={sanitizeAppPath(
        readSearchParam(resolvedSearchParams?.returnPath),
        "/account",
      )}
      {...storefrontProps}
    />
  );
}
