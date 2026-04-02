import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { InvoiceDetailPage } from "@/components/member/invoice-detail-page";
import { getCurrentMemberInvoice } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { buildNoIndexMetadata } from "@/lib/seo";

export async function generateMetadata({ params }: InvoiceDetailRouteProps) {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);
  const { id } = await params;

  return buildNoIndexMetadata(
    culture,
    copy.invoiceDetailMetaTitle,
    undefined,
    `/invoices/${id}`,
  );
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
  const session = await getMemberSession();
  const { id } = await params;
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  if (!session) {
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.invoiceDetailAuthRequiredTitle}
        message={copy.invoiceDetailAuthRequiredMessage}
        returnPath={`/invoices/${id}`}
      />
    );
  }

  const invoiceResult = await getCurrentMemberInvoice(id);

  return (
    <InvoiceDetailPage
      culture={culture}
      invoice={invoiceResult.data}
      status={invoiceResult.status}
      paymentError={readSearchParam(resolvedSearchParams?.paymentError)}
    />
  );
}
