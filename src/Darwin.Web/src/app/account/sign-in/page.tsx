import { SignInPage } from "@/components/account/sign-in-page";
import { getPublicSignInPageContext } from "@/features/account/server/get-public-auth-page-context";
import { getPublicSignInSeoMetadata } from "@/features/account/server/get-public-auth-seo-metadata";
import { getExternalLoginConfig } from "@/features/member-session/external-login";
import { sanitizeAppPath } from "@/lib/locale-routing";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getPublicSignInSeoMetadata(culture);
  return metadata;
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
  const [{ storefrontProps }, externalLogin] = await Promise.all([
    getPublicSignInPageContext(culture),
    getExternalLoginConfig(),
  ]);

  return (
    <SignInPage
      culture={culture}
      email={readSearchParam(resolvedSearchParams?.email)}
      signInError={readSearchParam(resolvedSearchParams?.signInError)}
      returnPath={sanitizeAppPath(readSearchParam(resolvedSearchParams?.returnPath), "/account")}
      externalLogin={externalLogin}
      {...storefrontProps}
    />
  );
}
