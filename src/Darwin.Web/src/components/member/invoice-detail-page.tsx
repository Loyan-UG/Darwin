import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { createMemberInvoicePaymentIntentAction } from "@/features/member-portal/actions";
import type { MemberInvoiceDetail } from "@/features/member-portal/types";
import {
  formatResource,
  getMemberResource,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { buildInvoicePath, buildOrderPath } from "@/lib/entity-paths";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { localizeHref } from "@/lib/locale-routing";
import { getSafeExternalLinkProps, toWebApiUrl } from "@/lib/webapi-url";

type InvoiceDetailPageProps = {
  culture: string;
  invoice: MemberInvoiceDetail | null;
  status: string;
  paymentError?: string;
};

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

export function InvoiceDetailPage({
  culture,
  invoice,
  paymentError,
}: InvoiceDetailPageProps) {
  const copy = getMemberResource(culture);
  const resolvedPaymentError = resolveLocalizedQueryMessage(paymentError, copy);

  if (!invoice) {
    return (
      <section className="mx-auto flex w-full max-w-[1180px] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <div className="w-full rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.invoiceDetailUnavailableTitle}
            message={copy.invoiceDetailUnavailableCustomerMessage}
          />
          <div className="mt-8 flex flex-wrap gap-3">
            <Link
              href={localizeHref("/invoices", culture)}
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {copy.backToInvoicesCta}
            </Link>
            <Link
              href={localizeHref("/account", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.memberBreadcrumbAccount}
            </Link>
          </div>
        </div>
      </section>
    );
  }

  const documentPath = invoice.actions.documentPath
    ? `${invoice.actions.documentPath}?culture=${encodeURIComponent(culture)}`
    : null;
  const documentUrl = documentPath ? toWebApiUrl(documentPath) : "";
  const safeExternalLinkProps = getSafeExternalLinkProps();

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
            <Link href={localizeHref("/invoices", culture)} className="transition hover:text-[var(--color-brand)]">
              {copy.memberBreadcrumbInvoices}
            </Link>
            <span>/</span>
            <span className="font-medium text-[var(--color-text-primary)]">
              {invoice.orderNumber ?? invoice.id}
            </span>
          </nav>

          <header className="rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_48%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
              {copy.invoiceDetailEyebrow}
            </p>
            <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
              {invoice.orderNumber ?? invoice.id}
            </h1>
            <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.invoiceDetailPortalNote}
            </p>
          </header>

          {resolvedPaymentError ? (
            <StatusBanner
              tone="warning"
              title={copy.paymentRetryFailedTitle}
              message={resolvedPaymentError}
            />
          ) : null}

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.invoiceLinesTitle}
            </p>
            <div className="mt-5 grid gap-4">
              {invoice.lines.map((line) => (
                <article
                  key={line.id}
                  className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                >
                  <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                      <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">
                        {line.description}
                      </h2>
                      <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {formatResource(copy.qtyTaxRateLabel, {
                          quantity: line.quantity,
                          taxRate: line.taxRate,
                        })}
                      </p>
                    </div>
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {formatMoney(line.totalGrossMinor, invoice.currency, culture)}
                    </p>
                  </div>
                </article>
              ))}
            </div>
          </section>

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                  {copy.paymentSummaryTitle}
                </p>
                <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.paymentSummaryDescription}
                </p>
              </div>
              {documentUrl ? (
                <a
                  href={documentUrl}
                  {...safeExternalLinkProps}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.downloadDocumentCta}
                </a>
              ) : (
                <span className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-secondary)]">
                  {copy.documentUnavailableLabel}
                </span>
              )}
            </div>
            <div className="mt-5 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              {invoice.paymentSummary}
            </div>
          </section>
        </div>

        <aside className="flex flex-col gap-5">
          <MemberPortalNav culture={culture} activePath="/invoices" />

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.summaryTitle}
            </p>
            <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
              <div className="flex items-center justify-between gap-4">
                <span>{copy.statusLabel}</span>
                <span>{localizeInvoiceStatus(invoice.status, culture)}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span>{copy.createdLabel}</span>
                <span>{formatDateTime(invoice.createdAtUtc, culture)}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span>{copy.dueDateLabel}</span>
                <span>{formatDateTime(invoice.dueDateUtc, culture)}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span>{copy.netLabel}</span>
                <span>{formatMoney(invoice.totalNetMinor, invoice.currency, culture)}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span>{copy.taxLabel}</span>
                <span>{formatMoney(invoice.totalTaxMinor, invoice.currency, culture)}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span>{copy.settledLabel}</span>
                <span>{formatMoney(invoice.settledAmountMinor, invoice.currency, culture)}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span>{copy.balanceOnlyLabel}</span>
                <span>{formatMoney(invoice.balanceMinor, invoice.currency, culture)}</span>
              </div>
              <div className="flex items-center justify-between gap-4 border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]">
                <span>{copy.totalLabel}</span>
                <span>{formatMoney(invoice.totalGrossMinor, invoice.currency, culture)}</span>
              </div>
            </div>
          </section>

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.actionsTitle}
            </p>
            <div className="mt-4 flex flex-col gap-3">
              {invoice.actions.canRetryPayment ? (
                <form action={createMemberInvoicePaymentIntentAction}>
                  <input type="hidden" name="invoiceId" value={invoice.id} />
                  <input type="hidden" name="culture" value={culture} />
                  <input
                    type="hidden"
                    name="failurePath"
                    value={localizeHref(buildInvoicePath(invoice.id), culture)}
                  />
                  <button
                    type="submit"
                    className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                  >
                    {copy.retryPaymentCta}
                  </button>
                </form>
              ) : null}
              {invoice.orderId ? (
                <Link
                  href={localizeHref(buildOrderPath(invoice.orderId), culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.openLinkedOrderCta}
                </Link>
              ) : null}
              {documentUrl ? (
                <a
                  href={documentUrl}
                  {...safeExternalLinkProps}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.downloadDocumentCta}
                </a>
              ) : (
                <span className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-secondary)]">
                  {copy.documentUnavailableLabel}
                </span>
              )}
              <Link
                href={localizeHref("/invoices", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.backToInvoicesCta}
              </Link>
            </div>
          </section>
        </aside>
      </div>
    </section>
  );
}
