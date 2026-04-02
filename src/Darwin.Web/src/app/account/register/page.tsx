import { RegisterPage } from "@/components/account/register-page";
import { sanitizeAppPath } from "@/lib/locale-routing";
import { getRequestCulture } from "@/lib/request-culture";
import { buildNoIndexMetadata } from "@/lib/seo";
import { getMemberResource } from "@/localization";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);

  return buildNoIndexMetadata(
    culture,
    copy.registerMetaTitle,
    undefined,
    "/account/register",
  );
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
    />
  );
}
