import { ProfilePage } from "@/components/account/profile-page";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getMemberEditorPageContext } from "@/features/member-portal/server/get-member-protected-page-context";
import { getProfileSeoMetadata } from "@/features/member-portal/server/get-member-route-seo-metadata";
import {
  createStorefrontCartProps,
} from "@/features/storefront/route-projections";
import { getMemberResource } from "@/localization";
import { getRequestCulture, getSupportedCulturesAsync } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getProfileSeoMetadata(culture);
  return metadata;
}

type ProfileRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function ProfileRoute({ searchParams }: ProfileRouteProps) {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const supportedCultures = await getSupportedCulturesAsync();
  const { entryContext, routeContext } = await getMemberEditorPageContext(
    culture,
    "/account/profile",
  );
  const { session, storefrontContext: authStorefrontContext } = entryContext;

  if (!session) {
    const storefrontProps =
      createStorefrontCartProps(authStorefrontContext!);
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.profileAuthRequiredTitle}
        message={copy.profileAuthRequiredMessage}
        returnPath="/account/profile"
        {...storefrontProps}
      />
    );
  }

  const { identityContext } = routeContext!;

  return (
    <ProfilePage
      culture={culture}
      profile={identityContext.profileResult.data}
      supportedCultures={supportedCultures}
      status={identityContext.profileResult.status}
      profileStatus={readSearchParam(resolvedSearchParams?.profileStatus)}
      profileError={readSearchParam(resolvedSearchParams?.profileError)}
      phoneStatus={readSearchParam(resolvedSearchParams?.phoneStatus)}
      phoneError={readSearchParam(resolvedSearchParams?.phoneError)}
    />
  );
}
