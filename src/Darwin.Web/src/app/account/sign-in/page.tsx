import { SignInPage } from "@/components/account/sign-in-page";
import { sanitizeAppPath } from "@/lib/locale-routing";
import { getRequestCulture } from "@/lib/request-culture";
import { buildNoIndexMetadata } from "@/lib/seo";
import { getMemberResource } from "@/localization";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);

  return buildNoIndexMetadata(
    culture,
    copy.signInMetaTitle,
    undefined,
    "/account/sign-in",
  );
}

type SignInRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function SignInRoute({ searchParams }: SignInRouteProps) {
  const culture = await getRequestCulture();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  return (
    <SignInPage
      culture={culture}
      email={readSearchParam(resolvedSearchParams?.email)}
      signInError={readSearchParam(resolvedSearchParams?.signInError)}
      returnPath={sanitizeAppPath(readSearchParam(resolvedSearchParams?.returnPath), "/account")}
    />
  );
}
