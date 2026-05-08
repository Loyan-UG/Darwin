import { SecurityPage } from "@/components/account/security-page";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getMemberEditorPageContext } from "@/features/member-portal/server/get-member-protected-page-context";
import { getSecuritySeoMetadata } from "@/features/member-portal/server/get-member-route-seo-metadata";
import {
  createStorefrontCartProps,
} from "@/features/storefront/route-projections";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getSecuritySeoMetadata(culture);
  return metadata;
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
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const { entryContext, routeContext } = await getMemberEditorPageContext(
    culture,
    "/account/security",
  );
  const { session, storefrontContext: authStorefrontContext } = entryContext;

  if (!session) {
    const storefrontProps =
      createStorefrontCartProps(authStorefrontContext!);
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.securityAuthRequiredTitle}
        message={copy.securityAuthRequiredMessage}
        returnPath="/account/security"
        {...storefrontProps}
      />
    );
  }

  const { identityContext } = routeContext!;

  return (
    <SecurityPage
      culture={culture}
      session={session}
      profile={identityContext.profileResult.data}
      profileStatus={identityContext.profileResult.status}
      securityStatus={readSearchParam(resolvedSearchParams?.securityStatus)}
      securityError={readSearchParam(resolvedSearchParams?.securityError)}
    />
  );
}
