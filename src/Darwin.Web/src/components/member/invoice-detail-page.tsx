import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { createMemberInvoicePaymentIntentAction } from "@/features/member-portal/actions";
import type { MemberInvoiceDetail } from "@/features/member-portal/types";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { toWebApiUrl } from "@/lib/webapi-url";

type InvoiceDetailPageProps = {
  culture: string;
  invoice: MemberInvoiceDetail | null;
  status: string;
  paymentError?: string;
};

export function InvoiceDetailPage({
  culture,
  invoice,
  status,
  paymentError,
}: InvoiceDetailPageProps) {
  if (!invoice) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <div className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner tone="warning" title="Invoice detail is unavailable." message={`The member invoice endpoint returned status "${status}".`} />
        </div>
      </section>
    );
  }

  const documentUrl = toWebApiUrl(invoice.actions.documentPath);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">Invoice detail</p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {invoice.orderNumber ?? invoice.id}
          </h1>
        </div>

        {paymentError && (
          <StatusBanner tone="warning" title="Payment retry failed" message={paymentError} />
        )}

        <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
          <div className="flex flex-col gap-6">
            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">Invoice lines</p>
              <div className="mt-5 flex flex-col gap-4">
                {invoice.lines.map((line) => (
                  <article key={line.id} className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <div className="flex flex-wrap items-start justify-between gap-4">
                      <div>
                        <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">{line.description}</h2>
                        <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                          Qty: {line.quantity} | Tax rate: {line.taxRate}
                        </p>
                      </div>
                      <p className="text-sm font-semibold text-[var(--color-text-primary)]">{formatMoney(line.totalGrossMinor, invoice.currency, culture)}</p>
                    </div>
                  </article>
                ))}
              </div>
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">Payment summary</p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    Billing presentation now consumes the canonical summary fields and document link exposed by the member invoice contract.
                  </p>
                </div>
                <a
                  href={documentUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  Download document
                </a>
              </div>
              <div className="mt-5 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                {invoice.paymentSummary}
              </div>
            </div>
          </div>

          <div className="flex flex-col gap-5">
            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">Summary</p>
              <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
                <div className="flex items-center justify-between"><span>Status</span><span>{invoice.status}</span></div>
                <div className="flex items-center justify-between"><span>Created</span><span>{formatDateTime(invoice.createdAtUtc, culture)}</span></div>
                <div className="flex items-center justify-between"><span>Due date</span><span>{formatDateTime(invoice.dueDateUtc, culture)}</span></div>
                <div className="flex items-center justify-between"><span>Net</span><span>{formatMoney(invoice.totalNetMinor, invoice.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>Tax</span><span>{formatMoney(invoice.totalTaxMinor, invoice.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>Settled</span><span>{formatMoney(invoice.settledAmountMinor, invoice.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>Balance</span><span>{formatMoney(invoice.balanceMinor, invoice.currency, culture)}</span></div>
                <div className="flex items-center justify-between border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]"><span>Total</span><span>{formatMoney(invoice.totalGrossMinor, invoice.currency, culture)}</span></div>
              </div>
            </aside>

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">Actions</p>
              <div className="mt-4 flex flex-col gap-3">
                {invoice.actions.canRetryPayment && (
                  <form action={createMemberInvoicePaymentIntentAction}>
                    <input type="hidden" name="invoiceId" value={invoice.id} />
                    <input type="hidden" name="failurePath" value={`/invoices/${invoice.id}`} />
                    <button type="submit" className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
                      Retry payment
                    </button>
                  </form>
                )}
                {invoice.orderId ? (
                  <Link href={`/orders/${invoice.orderId}`} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                    Open linked order
                  </Link>
                ) : null}
                <a href={documentUrl} target="_blank" rel="noreferrer" className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  Download document
                </a>
                <Link href="/invoices" className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  Back to invoices
                </Link>
              </div>
            </aside>
          </div>
        </div>
      </div>
    </section>
  );
}
