import { MemberAuthRequired } from "@/components/member/member-auth-required";
import {
  readAllowedSearchParam,
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
import { InvoicesPage } from "@/components/member/invoices-page";
import { getMemberInvoicesPageContext } from "@/features/member-portal/server/get-member-protected-page-context";
import { getInvoicesSeoMetadata } from "@/features/member-portal/server/get-member-route-seo-metadata";
import { createStorefrontCartProps } from "@/features/storefront/route-projections";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getInvoicesSeoMetadata(culture);
  return metadata;
}

type InvoicesRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function InvoicesRoute({
  searchParams,
}: InvoicesRouteProps) {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);
  const visibleQuery = readSearchTextParam(resolvedSearchParams?.visibleQuery);
  const visibleState = readAllowedSearchParam(
    resolvedSearchParams?.visibleState,
    ["all", "outstanding", "settled"] as const,
  );
  const { entryContext, routeContext } = await getMemberInvoicesPageContext(
    culture,
    safePage,
    12,
  );
  const { session, storefrontContext: authStorefrontContext } = entryContext;

  if (!session) {
    const storefrontProps = createStorefrontCartProps(authStorefrontContext!);
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.invoicesAuthRequiredTitle}
        message={copy.invoicesAuthRequiredMessage}
        returnPath="/invoices"
        {...storefrontProps}
      />
    );
  }

  const { invoicesResult } = routeContext!;

  return (
    <InvoicesPage
      culture={culture}
      invoices={invoicesResult.data?.items ?? []}
      status={invoicesResult.status}
      currentPage={safePage}
      totalPages={Math.max(1, Math.ceil((invoicesResult.data?.total ?? 0) / (invoicesResult.data?.request.pageSize ?? 12)))}
      visibleQuery={visibleQuery}
      visibleState={visibleState ?? "all"}
    />
  );
}
