import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { InvoicesPage } from "@/components/member/invoices-page";
import { getCurrentMemberInvoices } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getRequestCulture } from "@/lib/request-culture";

export const metadata = {
  title: "Invoices",
};

type InvoicesRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function InvoicesRoute({
  searchParams,
}: InvoicesRouteProps) {
  const culture = await getRequestCulture();
  const session = await getMemberSession();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const page = Number((Array.isArray(resolvedSearchParams?.page) ? resolvedSearchParams?.page[0] : resolvedSearchParams?.page) ?? "1");
  const safePage = Number.isFinite(page) && page > 0 ? page : 1;

  if (!session) {
    return (
      <MemberAuthRequired
        title="Member sign-in is required for invoice history."
        message="Invoices now live behind the authenticated member portal and no longer use a placeholder route."
        returnPath="/invoices"
      />
    );
  }

  const invoicesResult = await getCurrentMemberInvoices({
    page: safePage,
    pageSize: 12,
  });

  return (
    <InvoicesPage
      culture={culture}
      invoices={invoicesResult.data?.items ?? []}
      status={invoicesResult.status}
      currentPage={safePage}
      totalPages={Math.max(1, Math.ceil((invoicesResult.data?.total ?? 0) / (invoicesResult.data?.request.pageSize ?? 12)))}
    />
  );
}
