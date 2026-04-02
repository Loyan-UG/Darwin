import { AddressesPage } from "@/components/account/addresses-page";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getCurrentMemberAddresses } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { buildNoIndexMetadata } from "@/lib/seo";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);

  return buildNoIndexMetadata(
    culture,
    copy.addressesMetaTitle,
    undefined,
    "/account/addresses",
  );
}

type AddressesRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function AddressesRoute({
  searchParams,
}: AddressesRouteProps) {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);
  const session = await getMemberSession();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  if (!session) {
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.addressesAuthRequiredTitle}
        message={copy.addressesAuthRequiredMessage}
        returnPath="/account/addresses"
      />
    );
  }

  const addressesResult = await getCurrentMemberAddresses();

  return (
    <AddressesPage
      culture={culture}
      addresses={addressesResult.data ?? []}
      status={addressesResult.status}
      addressesStatus={readSearchParam(resolvedSearchParams?.addressesStatus)}
      addressesError={readSearchParam(resolvedSearchParams?.addressesError)}
    />
  );
}
