import { AccountHubPage } from "@/components/account/account-hub-page";
import { MemberDashboardPage } from "@/components/account/member-dashboard-page";
import {
  getCurrentMemberCustomerContext,
  getCurrentMemberPreferences,
  getCurrentMemberProfile,
} from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getRequestCulture } from "@/lib/request-culture";
import { buildNoIndexMetadata } from "@/lib/seo";
import { getMemberResource } from "@/localization";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);

  return buildNoIndexMetadata(culture, copy.accountMetaTitle, undefined, "/account");
}

export default async function AccountPage() {
  const culture = await getRequestCulture();
  const session = await getMemberSession();
  if (!session) {
    return <AccountHubPage culture={culture} />;
  }

  const [profileResult, preferencesResult, customerContextResult] = await Promise.all([
    getCurrentMemberProfile(),
    getCurrentMemberPreferences(),
    getCurrentMemberCustomerContext(),
  ]);

  return (
    <MemberDashboardPage
      culture={culture}
      session={session}
      profile={profileResult.data}
      profileStatus={profileResult.status}
      preferences={preferencesResult.data}
      preferencesStatus={preferencesResult.status}
      customerContext={customerContextResult.data}
      customerContextStatus={customerContextResult.status}
    />
  );
}
