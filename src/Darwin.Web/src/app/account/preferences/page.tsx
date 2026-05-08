import { PreferencesPage } from "@/components/account/preferences-page";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getMemberEditorPageContext } from "@/features/member-portal/server/get-member-protected-page-context";
import { getPreferencesSeoMetadata } from "@/features/member-portal/server/get-member-route-seo-metadata";
import {
  createStorefrontCartProps,
} from "@/features/storefront/route-projections";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getPreferencesSeoMetadata(culture);
  return metadata;
}

type PreferencesRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function PreferencesRoute({
  searchParams,
}: PreferencesRouteProps) {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const { entryContext, routeContext } = await getMemberEditorPageContext(
    culture,
    "/account/preferences",
  );
  const { session, storefrontContext: authStorefrontContext } = entryContext;

  if (!session) {
    const storefrontProps =
      createStorefrontCartProps(authStorefrontContext!);
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.preferencesAuthRequiredTitle}
        message={copy.preferencesAuthRequiredMessage}
        returnPath="/account/preferences"
        {...storefrontProps}
      />
    );
  }

  const { identityContext } = routeContext!;

  return (
    <PreferencesPage
      culture={culture}
      preferences={identityContext.preferencesResult.data}
      status={identityContext.preferencesResult.status}
      profile={identityContext.profileResult.data}
      profileStatus={identityContext.profileResult.status}
      preferencesStatus={readSearchParam(resolvedSearchParams?.preferencesStatus)}
      preferencesError={readSearchParam(resolvedSearchParams?.preferencesError)}
    />
  );
}
