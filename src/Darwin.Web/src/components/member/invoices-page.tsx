import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { MemberInvoiceSummary } from "@/features/member-portal/types";
import {
  formatResource,
  getMemberResource,
  resolveApiStatusLabel,
} from "@/localization";
import { buildInvoicePath } from "@/lib/entity-paths";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";

type InvoicesPageProps = {
  culture: string;
  invoices: MemberInvoiceSummary[];
  status: string;
  currentPage: number;
  totalPages: number;
  visibleQuery?: string;
  visibleState: "all" | "outstanding" | "settled";
};

function isOutstandingInvoice(invoice: MemberInvoiceSummary) {
  return invoice.balanceMinor > 0;
}

function localizeInvoiceStatus(status: string, culture: string) {
  if (culture.toLowerCase().startsWith("en")) {
    return status;
  }

  const labels: Record<string, string> = {
    Draft: "Entwurf",
    Open: "Offen",
    Paid: "Bezahlt",
    Cancelled: "Storniert",
  };

  return labels[status] ?? status;
}

function buildInvoicesHref(
  page = 1,
  options?: {
    visibleQuery?: string;
    visibleState?: "all" | "outstanding" | "settled";
  },
) {
  return buildAppQueryPath("/invoices", {
    page: page > 1 ? page : undefined,
    visibleQuery: options?.visibleQuery,
    visibleState:
      options?.visibleState && options.visibleState !== "all"
        ? options.visibleState
        : undefined,
  });
}

export function InvoicesPage({
  culture,
  invoices,
  status,
  currentPage,
  totalPages,
  visibleQuery,
  visibleState,
}: InvoicesPageProps) {
  const copy = getMemberResource(culture);
  const statusLabel = resolveApiStatusLabel(status, copy) ?? status;
  const normalizedVisibleQuery = visibleQuery?.trim().toLowerCase() ?? "";
  const filteredInvoices = invoices.filter((invoice) => {
    const matchesQuery =
      normalizedVisibleQuery.length === 0 ||
      [
        invoice.orderNumber ?? "",
        invoice.status,
        localizeInvoiceStatus(invoice.status, culture),
        invoice.id,
      ].some((value) => value.toLowerCase().includes(normalizedVisibleQuery));
    const matchesState =
      visibleState === "all" ||
      (visibleState === "outstanding" && isOutstandingInvoice(invoice)) ||
      (visibleState === "settled" && !isOutstandingInvoice(invoice));

    return matchesQuery && matchesState;
  });

  return (
    <section className="mx-auto flex w-full max-w-[1180px] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-8 lg:grid-cols-[minmax(0,1fr)_320px]">
        <div className="flex flex-col gap-8">
          <nav
            aria-label={copy.memberBreadcrumbLabel}
            className="flex flex-wrap items-center gap-2 text-sm text-[var(--color-text-secondary)]"
          >
            <Link href={localizeHref("/", culture)} className="transition hover:text-[var(--color-brand)]">
              {copy.memberBreadcrumbHome}
            </Link>
            <span>/</span>
            <Link href={localizeHref("/account", culture)} className="transition hover:text-[var(--color-brand)]">
              {copy.memberBreadcrumbAccount}
            </Link>
            <span>/</span>
            <span className="font-medium text-[var(--color-text-primary)]">
              {copy.memberBreadcrumbInvoices}
            </span>
          </nav>

          <header className="rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_48%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
              {copy.invoicesEyebrow}
            </p>
            <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
              {copy.invoicesTitle}
            </h1>
            <p className="mt-4 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.invoicesPortalNote}
            </p>
          </header>

          {status !== "ok" ? (
            <StatusBanner
              tone="warning"
              title={copy.invoicesWarningsTitle}
              message={formatResource(copy.invoicesWarningsMessage, { status: statusLabel })}
            />
          ) : null}

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.invoicesFilterTitle}
              </p>
              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.invoicesFilterMessage, {
                  count: filteredInvoices.length,
                })}
              </p>
            </div>
            <form
              action={localizeHref("/invoices", culture)}
              method="get"
              className="mt-5 grid gap-4 lg:grid-cols-[minmax(0,1fr)_220px_auto]"
            >
              <label className="flex flex-col gap-2 text-sm text-[var(--color-text-secondary)]">
                <span>{copy.invoicesFilterSearchPlaceholder}</span>
                <input
                  type="search"
                  name="visibleQuery"
                  defaultValue={visibleQuery ?? ""}
                  placeholder={copy.invoicesFilterSearchPlaceholder}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-base)] px-4 py-3 text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
                />
              </label>
              <label className="flex flex-col gap-2 text-sm text-[var(--color-text-secondary)]">
                <span>{copy.invoicesFilterStateLabel}</span>
                <select
                  name="visibleState"
                  defaultValue={visibleState}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-base)] px-4 py-3 text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
                >
                  <option value="all">{copy.invoicesFilterStateAll}</option>
                  <option value="outstanding">{copy.invoicesFilterStateOutstanding}</option>
                  <option value="settled">{copy.invoicesFilterStateSettled}</option>
                </select>
              </label>
              <div className="flex items-end gap-3">
                <button
                  type="submit"
                  className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-white transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.invoicesFilterApplyCta}
                </button>
                {(visibleQuery || visibleState !== "all") ? (
                  <Link
                    href={localizeHref("/invoices", culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.clearFilterCta}
                  </Link>
                ) : null}
              </div>
            </form>
          </section>

          <section className="grid gap-5">
            {filteredInvoices.map((invoice) => (
              <article
                key={invoice.id}
                className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
              >
                <div className="flex flex-wrap items-start justify-between gap-4">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                      {localizeInvoiceStatus(invoice.status, culture)}
                    </p>
                    <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                      <Link
                        href={localizeHref(buildInvoicePath(invoice.id), culture)}
                        className="transition hover:text-[var(--color-brand)]"
                      >
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
                      href={localizeHref(buildInvoicePath(invoice.id), culture)}
                      className="mt-3 inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                    >
                      {copy.openInvoiceCta}
                    </Link>
                  </div>
                </div>
              </article>
            ))}
          </section>

          {invoices.length === 0 ? (
            <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center">
              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.noInvoicesMessage}
              </p>
              <Link
                href={localizeHref("/catalog", culture)}
                className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
              >
                {copy.memberCrossSurfaceCatalogCta}
              </Link>
            </div>
          ) : null}

          {invoices.length > 0 && filteredInvoices.length === 0 ? (
            <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center">
              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.invoicesFilterEmptyMessage}
              </p>
            </div>
          ) : null}

          {totalPages > 1 ? (
            <div className="flex flex-wrap items-center gap-3">
              <Link
                aria-disabled={currentPage <= 1}
                href={localizeHref(
                  buildInvoicesHref(Math.max(1, currentPage - 1), {
                    visibleQuery,
                    visibleState,
                  }),
                  culture,
                )}
                className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
              >
                {copy.previous}
              </Link>
              <p className="text-sm text-[var(--color-text-secondary)]">
                {formatResource(copy.pageLabel, { currentPage, totalPages })}
              </p>
              <Link
                aria-disabled={currentPage >= totalPages}
                href={localizeHref(
                  buildInvoicesHref(Math.min(totalPages, currentPage + 1), {
                    visibleQuery,
                    visibleState,
                  }),
                  culture,
                )}
                className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
              >
                {copy.next}
              </Link>
            </div>
          ) : null}
        </div>

        <aside className="flex flex-col gap-6">
          <MemberPortalNav culture={culture} activePath="/invoices" />
          <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.invoicesRouteLabel}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.invoicesPortalNote}
            </p>
          </div>
        </aside>
      </div>
    </section>
  );
}
