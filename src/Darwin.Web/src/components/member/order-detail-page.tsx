import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { createMemberOrderPaymentIntentAction } from "@/features/member-portal/actions";
import type { MemberOrderDetail } from "@/features/member-portal/types";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { toWebApiUrl } from "@/lib/webapi-url";

type ParsedAddress = {
  fullName?: string;
  company?: string | null;
  street1?: string;
  street2?: string | null;
  postalCode?: string;
  city?: string;
  state?: string | null;
  countryCode?: string;
  phoneE164?: string | null;
};

type OrderDetailPageProps = {
  culture: string;
  order: MemberOrderDetail | null;
  status: string;
  paymentError?: string;
};

function parseAddress(rawJson: string): ParsedAddress | null {
  try {
    const parsed = JSON.parse(rawJson) as ParsedAddress;
    return parsed && typeof parsed === "object" ? parsed : null;
  } catch {
    return null;
  }
}

function renderAddress(address: ParsedAddress | null) {
  if (!address) {
    return <p className="text-sm leading-7 text-[var(--color-text-secondary)]">Snapshot unavailable.</p>;
  }

  return (
    <div className="text-sm leading-7 text-[var(--color-text-secondary)]">
      <p className="font-semibold text-[var(--color-text-primary)]">{address.fullName ?? "Recipient unavailable"}</p>
      {address.company ? <p>{address.company}</p> : null}
      <p>{address.street1}</p>
      {address.street2 ? <p>{address.street2}</p> : null}
      <p>{address.postalCode} {address.city}</p>
      {address.state ? <p>{address.state}</p> : null}
      <p>{address.countryCode}</p>
    </div>
  );
}

export function OrderDetailPage({
  culture,
  order,
  status,
  paymentError,
}: OrderDetailPageProps) {
  if (!order) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <div className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner tone="warning" title="Order detail is unavailable." message={`The member order endpoint returned status "${status}".`} />
        </div>
      </section>
    );
  }

  const documentUrl = toWebApiUrl(order.actions.documentPath);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">Order detail</p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {order.orderNumber}
          </h1>
        </div>

        {paymentError && (
          <StatusBanner tone="warning" title="Payment retry failed" message={paymentError} />
        )}

        <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
          <div className="flex flex-col gap-6">
            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <div className="grid gap-5 sm:grid-cols-2">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">Billing</p>
                  <div className="mt-3">{renderAddress(parseAddress(order.billingAddressJson))}</div>
                </div>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">Shipping</p>
                  <div className="mt-3">{renderAddress(parseAddress(order.shippingAddressJson))}</div>
                </div>
              </div>
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">Order lines</p>
              <div className="mt-5 flex flex-col gap-4">
                {order.lines.map((line) => (
                  <article key={line.id} className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <div className="flex flex-wrap items-start justify-between gap-4">
                      <div>
                        <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">{line.name}</h2>
                        <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">SKU: {line.sku} | Qty: {line.quantity}</p>
                      </div>
                      <p className="text-sm font-semibold text-[var(--color-text-primary)]">{formatMoney(line.lineGrossMinor, order.currency, culture)}</p>
                    </div>
                  </article>
                ))}
              </div>
            </div>

            <div className="grid gap-6 xl:grid-cols-2">
              <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">Payments</p>
                {order.payments.length > 0 ? (
                  <div className="mt-5 flex flex-col gap-4">
                    {order.payments.map((payment) => (
                      <article key={payment.id} className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                        <div className="flex flex-wrap items-start justify-between gap-4">
                          <div>
                            <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                              {payment.provider}
                            </p>
                            <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                              {payment.status}
                            </p>
                            {payment.providerReference ? (
                              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                                Reference: {payment.providerReference}
                              </p>
                            ) : null}
                            {payment.paidAtUtc ? (
                              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                                Paid: {formatDateTime(payment.paidAtUtc, culture)}
                              </p>
                            ) : null}
                          </div>
                          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                            {formatMoney(payment.amountMinor, payment.currency, culture)}
                          </p>
                        </div>
                      </article>
                    ))}
                  </div>
                ) : (
                  <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                    No payment attempts are attached to this order yet.
                  </p>
                )}
              </div>

              <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">Shipments</p>
                {order.shipments.length > 0 ? (
                  <div className="mt-5 flex flex-col gap-4">
                    {order.shipments.map((shipment) => (
                      <article key={shipment.id} className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                        <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                          {shipment.carrier} / {shipment.service}
                        </p>
                        <div className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                          <p>Status: {shipment.status}</p>
                          {shipment.trackingNumber ? <p>Tracking: {shipment.trackingNumber}</p> : null}
                          {shipment.shippedAtUtc ? <p>Shipped: {formatDateTime(shipment.shippedAtUtc, culture)}</p> : null}
                          {shipment.deliveredAtUtc ? <p>Delivered: {formatDateTime(shipment.deliveredAtUtc, culture)}</p> : null}
                        </div>
                      </article>
                    ))}
                  </div>
                ) : (
                  <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                    No shipment snapshots are currently attached to this order.
                  </p>
                )}
              </div>
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">Linked invoices</p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    Order-linked invoice snapshots now come directly from the member order detail contract.
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

              {order.invoices.length > 0 ? (
                <div className="mt-5 flex flex-col gap-4">
                  {order.invoices.map((invoice) => (
                    <article key={invoice.id} className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                      <div className="flex flex-wrap items-start justify-between gap-4">
                        <div>
                          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                            {invoice.status}
                          </p>
                          <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                            Due: {formatDateTime(invoice.dueDateUtc, culture)}
                          </p>
                          {invoice.paidAtUtc ? (
                            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                              Paid: {formatDateTime(invoice.paidAtUtc, culture)}
                            </p>
                          ) : null}
                        </div>
                        <div className="text-right">
                          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                            {formatMoney(invoice.totalGrossMinor, invoice.currency, culture)}
                          </p>
                          <Link
                            href={`/invoices/${invoice.id}`}
                            className="mt-3 inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                          >
                            Open invoice
                          </Link>
                        </div>
                      </div>
                    </article>
                  ))}
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  No linked invoices are currently attached to this order.
                </p>
              )}
            </div>
          </div>

          <div className="flex flex-col gap-5">
            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">Summary</p>
              <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
                <div className="flex items-center justify-between"><span>Status</span><span>{order.status}</span></div>
                <div className="flex items-center justify-between"><span>Created</span><span>{formatDateTime(order.createdAtUtc, culture)}</span></div>
                <div className="flex items-center justify-between"><span>Subtotal</span><span>{formatMoney(order.subtotalNetMinor, order.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>Tax</span><span>{formatMoney(order.taxTotalMinor, order.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>Shipping</span><span>{formatMoney(order.shippingTotalMinor, order.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>Discount</span><span>{formatMoney(order.discountTotalMinor, order.currency, culture)}</span></div>
                <div className="flex items-center justify-between border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]"><span>Total</span><span>{formatMoney(order.grandTotalGrossMinor, order.currency, culture)}</span></div>
              </div>

              {(order.shippingMethodName || order.shippingCarrier || order.shippingService) && (
                <div className="mt-6 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  <p className="font-semibold text-[var(--color-text-primary)]">Shipping snapshot</p>
                  <p>{order.shippingMethodName ?? "Method unavailable"}</p>
                  <p>
                    {order.shippingCarrier ?? "Carrier unavailable"}
                    {order.shippingService ? ` / ${order.shippingService}` : ""}
                  </p>
                </div>
              )}
            </aside>

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">Actions</p>
              <div className="mt-4 flex flex-col gap-3">
                {order.actions.canRetryPayment && (
                  <form action={createMemberOrderPaymentIntentAction}>
                    <input type="hidden" name="orderId" value={order.id} />
                    <input type="hidden" name="failurePath" value={`/orders/${order.id}`} />
                    <button type="submit" className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
                      Retry payment
                    </button>
                  </form>
                )}
                <Link href={`/checkout/orders/${order.id}/confirmation?orderNumber=${encodeURIComponent(order.orderNumber)}`} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  Open confirmation
                </Link>
                <a href={documentUrl} target="_blank" rel="noreferrer" className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  Download document
                </a>
                <Link href="/orders" className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  Back to orders
                </Link>
              </div>
            </aside>
          </div>
        </div>
      </div>
    </section>
  );
}
