import { ProfilePage } from "@/components/account/profile-page";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getCurrentMemberProfile } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getSupportedCultures } from "@/lib/request-culture";

export const metadata = {
  title: "Profile",
};

type ProfileRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function ProfileRoute({ searchParams }: ProfileRouteProps) {
  const session = await getMemberSession();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const supportedCultures = getSupportedCultures();

  if (!session) {
    return (
      <MemberAuthRequired
        title="Member sign-in is required for profile editing."
        message="Profile editing now lives behind the authenticated member portal."
        returnPath="/account/profile"
      />
    );
  }

  const profileResult = await getCurrentMemberProfile();

  return (
    <ProfilePage
      profile={profileResult.data}
      supportedCultures={supportedCultures}
      status={profileResult.status}
      profileStatus={readSearchParam(resolvedSearchParams?.profileStatus)}
      profileError={readSearchParam(resolvedSearchParams?.profileError)}
      phoneStatus={readSearchParam(resolvedSearchParams?.phoneStatus)}
      phoneError={readSearchParam(resolvedSearchParams?.phoneError)}
    />
  );
}
