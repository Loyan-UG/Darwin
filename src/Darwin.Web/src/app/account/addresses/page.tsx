import { AddressesPage } from "@/components/account/addresses-page";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getMemberEditorPageContext } from "@/features/member-portal/server/get-member-protected-page-context";
import { getAddressesSeoMetadata } from "@/features/member-portal/server/get-member-route-seo-metadata";
import {
  createStorefrontCartProps,
} from "@/features/storefront/route-projections";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getAddressesSeoMetadata(culture);
  return metadata;
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
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const { entryContext, routeContext } = await getMemberEditorPageContext(
    culture,
    "/account/addresses",
  );
  const { session, storefrontContext: authStorefrontContext } = entryContext;

  if (!session) {
    const storefrontProps =
      createStorefrontCartProps(authStorefrontContext!);
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.addressesAuthRequiredTitle}
        message={copy.addressesAuthRequiredMessage}
        returnPath="/account/addresses"
        {...storefrontProps}
      />
    );
  }

  const { identityContext } = routeContext!;

  return (
    <AddressesPage
      culture={culture}
      addresses={identityContext.addressesResult.data ?? []}
      status={identityContext.addressesResult.status}
      addressesStatus={readSearchParam(resolvedSearchParams?.addressesStatus)}
      addressesError={readSearchParam(resolvedSearchParams?.addressesError)}
    />
  );
}
