import { AccountHubPage } from "@/components/account/account-hub-page";
import { MemberDashboardPage } from "@/components/account/member-dashboard-page";
import { getAccountPageView } from "@/features/account/server/get-account-page-view";
import { getPublicAccountSeoMetadata } from "@/features/account/server/get-public-auth-seo-metadata";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getPublicAccountSeoMetadata(culture);
  return metadata;
}

type AccountPageProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function AccountPage({ searchParams }: AccountPageProps) {
  const culture = await getRequestCulture();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const returnPath = Array.isArray(resolvedSearchParams?.returnPath)
    ? resolvedSearchParams?.returnPath[0]
    : resolvedSearchParams?.returnPath;
  const view = await getAccountPageView(culture, returnPath);

  return view.kind === "public" ? (
    <AccountHubPage {...view.props} />
  ) : (
    <MemberDashboardPage {...view.props} />
  );
}
