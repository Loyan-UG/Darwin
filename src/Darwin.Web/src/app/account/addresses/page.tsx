import { AddressesPage } from "@/components/account/addresses-page";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getCurrentMemberAddresses } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";

export const metadata = {
  title: "Addresses",
};

type AddressesRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function AddressesRoute({
  searchParams,
}: AddressesRouteProps) {
  const session = await getMemberSession();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  if (!session) {
    return (
      <MemberAuthRequired
        title="Member sign-in is required for address management."
        message="Reusable addresses now live behind the authenticated portal."
        returnPath="/account/addresses"
      />
    );
  }

  const addressesResult = await getCurrentMemberAddresses();

  return (
    <AddressesPage
      addresses={addressesResult.data ?? []}
      status={addressesResult.status}
      addressesStatus={readSearchParam(resolvedSearchParams?.addressesStatus)}
      addressesError={readSearchParam(resolvedSearchParams?.addressesError)}
    />
  );
}
