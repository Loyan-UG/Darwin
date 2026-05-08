import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { InvoiceDetailPage } from "@/components/member/invoice-detail-page";
import { getMemberInvoiceDetailPageContext } from "@/features/member-portal/server/get-member-protected-page-context";
import { getInvoiceDetailSeoMetadata } from "@/features/member-portal/server/get-member-route-seo-metadata";
import { createStorefrontCartProps } from "@/features/storefront/route-projections";
import { getMemberResource } from "@/localization";
import { buildInvoicePath } from "@/lib/entity-paths";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata({ params }: InvoiceDetailRouteProps) {
  const culture = await getRequestCulture();
  const { id } = await params;
  const { metadata } = await getInvoiceDetailSeoMetadata(culture, id);
  return metadata;
}

type InvoiceDetailRouteProps = {
  params: Promise<{
    id: string;
  }>;
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function InvoiceDetailRoute({
  params,
  searchParams,
}: InvoiceDetailRouteProps) {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);
  const { id } = await params;
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const { entryContext, routeContext } = await getMemberInvoiceDetailPageContext(
    culture,
    id,
  );
  const { session, storefrontContext: authStorefrontContext } = entryContext;

  if (!session) {
    const storefrontProps = createStorefrontCartProps(authStorefrontContext!);
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.invoiceDetailAuthRequiredTitle}
        message={copy.invoiceDetailAuthRequiredMessage}
        returnPath={buildInvoicePath(id)}
        {...storefrontProps}
      />
    );
  }

  const { invoiceResult } = routeContext!;

  return (
    <InvoiceDetailPage
      culture={culture}
      invoice={invoiceResult.data}
      status={invoiceResult.status}
      paymentError={readSearchParam(resolvedSearchParams?.paymentError)}
    />
  );
}
