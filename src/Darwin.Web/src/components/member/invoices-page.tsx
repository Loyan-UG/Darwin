import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { MemberInvoiceSummary } from "@/features/member-portal/types";
import { formatResource, getMemberResource } from "@/localization";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { localizeHref } from "@/lib/locale-routing";

type InvoicesPageProps = {
  culture: string;
  invoices: MemberInvoiceSummary[];
  status: string;
  currentPage: number;
  totalPages: number;
};

function buildInvoicesHref(page = 1) {
  return page > 1 ? `/invoices?page=${page}` : "/invoices";
}

export function InvoicesPage({
  culture,
  invoices,
  status,
  currentPage,
  totalPages,
}: InvoicesPageProps) {
  const copy = getMemberResource(culture);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-8 lg:grid-cols-[minmax(0,1fr)_320px]">
        <div className="flex flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.invoicesEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.invoicesTitle}
          </h1>
        </div>

        {status !== "ok" && (
          <StatusBanner
            tone="warning"
            title={copy.invoicesWarningsTitle}
            message={formatResource(copy.invoicesWarningsMessage, { status })}
          />
        )}

        <div className="grid gap-5">
          {invoices.map((invoice) => (
            <article
              key={invoice.id}
              className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
            >
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {invoice.status}
                  </p>
                  <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                    <Link href={localizeHref(`/invoices/${invoice.id}`, culture)} className="transition hover:text-[var(--color-brand)]">
                      {invoice.orderNumber ?? invoice.id}
                    </Link>
                  </h2>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.createdLabel} {formatDateTime(invoice.createdAtUtc, culture)}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-lg font-semibold text-[var(--color-text-primary)]">
                    {formatMoney(invoice.totalGrossMinor, invoice.currency, culture)}
                  </p>
                  <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                    {formatResource(copy.balanceLabel, {
                      value: formatMoney(invoice.balanceMinor, invoice.currency, culture),
                    })}
                  </p>
                  <Link
                    href={localizeHref(`/invoices/${invoice.id}`, culture)}
                    className="mt-3 inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.openInvoiceCta}
                  </Link>
                </div>
              </div>
            </article>
          ))}
        </div>

        {invoices.length === 0 && (
          <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.noInvoicesMessage}
          </div>
        )}

        {totalPages > 1 && (
          <div className="flex flex-wrap items-center gap-3">
            <Link
              aria-disabled={currentPage <= 1}
              href={localizeHref(buildInvoicesHref(Math.max(1, currentPage - 1)), culture)}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              {copy.previous}
            </Link>
            <p className="text-sm text-[var(--color-text-secondary)]">
              {formatResource(copy.pageLabel, { currentPage, totalPages })}
            </p>
            <Link
              aria-disabled={currentPage >= totalPages}
              href={localizeHref(buildInvoicesHref(Math.min(totalPages, currentPage + 1)), culture)}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              {copy.next}
            </Link>
          </div>
        )}

        </div>

        <div className="flex flex-col gap-6">
          <MemberPortalNav culture={culture} activePath="/invoices" />

          <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.invoicesRouteLabel}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.invoicesPortalNote}
            </p>
          </aside>
        </div>
      </div>
    </section>
  );
}
