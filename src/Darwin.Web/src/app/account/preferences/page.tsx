import { PreferencesPage } from "@/components/account/preferences-page";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getCurrentMemberPreferences } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { buildNoIndexMetadata } from "@/lib/seo";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);

  return buildNoIndexMetadata(
    culture,
    copy.preferencesMetaTitle,
    undefined,
    "/account/preferences",
  );
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
  const session = await getMemberSession();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  if (!session) {
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.preferencesAuthRequiredTitle}
        message={copy.preferencesAuthRequiredMessage}
        returnPath="/account/preferences"
      />
    );
  }

  const preferencesResult = await getCurrentMemberPreferences();

  return (
    <PreferencesPage
      culture={culture}
      preferences={preferencesResult.data}
      status={preferencesResult.status}
      preferencesStatus={readSearchParam(resolvedSearchParams?.preferencesStatus)}
      preferencesError={readSearchParam(resolvedSearchParams?.preferencesError)}
    />
  );
}
