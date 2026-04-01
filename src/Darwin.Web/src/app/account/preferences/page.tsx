import { PreferencesPage } from "@/components/account/preferences-page";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getCurrentMemberPreferences } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";

export const metadata = {
  title: "Preferences",
};

type PreferencesRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function PreferencesRoute({
  searchParams,
}: PreferencesRouteProps) {
  const session = await getMemberSession();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  if (!session) {
    return (
      <MemberAuthRequired
        title="Member sign-in is required for preference editing."
        message="Member preferences now live behind the authenticated portal."
        returnPath="/account/preferences"
      />
    );
  }

  const preferencesResult = await getCurrentMemberPreferences();

  return (
    <PreferencesPage
      preferences={preferencesResult.data}
      status={preferencesResult.status}
      preferencesStatus={readSearchParam(resolvedSearchParams?.preferencesStatus)}
      preferencesError={readSearchParam(resolvedSearchParams?.preferencesError)}
    />
  );
}
