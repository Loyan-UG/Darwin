import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { createStorefrontPaymentIntentAction } from "@/features/checkout/actions";
import type { PublicStorefrontOrderConfirmation } from "@/features/checkout/types";
import { formatDateTime, formatMoney } from "@/lib/formatting";

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

type OrderConfirmationPageProps = {
  confirmation: PublicStorefrontOrderConfirmation | null;
  status: string;
  message?: string;
  checkoutStatus?: string;
  paymentError?: string;
  cancelled?: boolean;
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
    return (
      <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
        Snapshot unavailable.
      </p>
    );
  }

  return (
    <div className="text-sm leading-7 text-[var(--color-text-secondary)]">
      <p className="font-semibold text-[var(--color-text-primary)]">
        {address.fullName || "Recipient unavailable"}
      </p>
      {address.company ? <p>{address.company}</p> : null}
      <p>{address.street1}</p>
      {address.street2 ? <p>{address.street2}</p> : null}
      <p>
        {address.postalCode} {address.city}
      </p>
      {address.state ? <p>{address.state}</p> : null}
      <p>{address.countryCode}</p>
      {address.phoneE164 ? <p>{address.phoneE164}</p> : null}
    </div>
  );
}

function hasSuccessfulPayment(confirmation: PublicStorefrontOrderConfirmation) {
  return confirmation.payments.some((payment) => {
    const status = payment.status.toLowerCase();
    return status === "paid" || status === "succeeded" || status === "completed";
  });
}

export function OrderConfirmationPage({
  confirmation,
  status,
  message,
  checkoutStatus,
  paymentError,
  cancelled,
}: OrderConfirmationPageProps) {
  if (!confirmation) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
        <div className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title="Order confirmation is unavailable."
            message={message ?? `The public confirmation endpoint returned status "${status}".`}
          />
          <div className="mt-8">
            <Link
              href="/catalog"
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              Back to catalog
            </Link>
          </div>
        </div>
      </section>
    );
  }

  const billingAddress = parseAddress(confirmation.billingAddressJson);
  const shippingAddress = parseAddress(confirmation.shippingAddressJson);
  const paid = hasSuccessfulPayment(confirmation);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            Order confirmation
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            Order {confirmation.orderNumber} is now on the storefront timeline
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            This screen reads the authoritative storefront confirmation contract from `Darwin.WebApi` and keeps payment handoff visible instead of hiding it behind placeholder text.
          </p>
        </div>

        {checkoutStatus === "order-placed" && (
          <StatusBanner
            title="Order placed"
            message="The cart was successfully converted into an order. The next step is payment handoff unless the order is already marked as paid."
          />
        )}

        {cancelled && (
          <StatusBanner
            tone="warning"
            title="Hosted checkout was cancelled."
            message="The shopper returned from the payment handoff without completing payment. The order remains visible here and payment can be retried."
          />
        )}

        {paymentError && (
          <StatusBanner
            tone="warning"
            title="Payment handoff failed"
            message={paymentError}
          />
        )}

        {status !== "ok" && (
          <StatusBanner
            tone="warning"
            title="Confirmation loaded with warnings."
            message={message ?? `Confirmation fetch returned status "${status}".`}
          />
        )}

        <div className="grid gap-8 lg:grid-cols-[minmax(0,1.05fr)_360px]">
          <div className="flex flex-col gap-6">
            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <div className="grid gap-5 sm:grid-cols-2">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    Billing address
                  </p>
                  <div className="mt-3">{renderAddress(billingAddress)}</div>
                </div>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    Shipping address
                  </p>
                  <div className="mt-3">{renderAddress(shippingAddress)}</div>
                </div>
              </div>
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                Order lines
              </p>
              <div className="mt-5 flex flex-col gap-4">
                {confirmation.lines.map((line) => (
                  <article
                    key={line.id}
                    className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                  >
                    <div className="flex flex-wrap items-start justify-between gap-4">
                      <div>
                        <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">
                          {line.name}
                        </h2>
                        <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                          SKU: {line.sku} | Qty: {line.quantity}
                        </p>
                      </div>
                      <div className="text-right text-sm leading-7 text-[var(--color-text-secondary)]">
                        <p>{formatMoney(line.unitPriceGrossMinor, confirmation.currency)} each</p>
                        <p className="font-semibold text-[var(--color-text-primary)]">
                          {formatMoney(line.lineGrossMinor, confirmation.currency)}
                        </p>
                      </div>
                    </div>
                  </article>
                ))}
              </div>
            </div>
          </div>

          <div className="flex flex-col gap-5">
            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                Summary
              </p>
              <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
                <div className="flex items-center justify-between">
                  <span>Status</span>
                  <span>{confirmation.status}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>Created</span>
                  <span>{formatDateTime(confirmation.createdAtUtc)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>Subtotal net</span>
                  <span>{formatMoney(confirmation.subtotalNetMinor, confirmation.currency)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>Tax total</span>
                  <span>{formatMoney(confirmation.taxTotalMinor, confirmation.currency)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>Shipping</span>
                  <span>{formatMoney(confirmation.shippingTotalMinor, confirmation.currency)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>Discount</span>
                  <span>{formatMoney(confirmation.discountTotalMinor, confirmation.currency)}</span>
                </div>
                <div className="flex items-center justify-between border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]">
                  <span>Grand total</span>
                  <span>{formatMoney(confirmation.grandTotalGrossMinor, confirmation.currency)}</span>
                </div>
              </div>

              {(confirmation.shippingMethodName || confirmation.shippingCarrier || confirmation.shippingService) && (
                <div className="mt-6 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  <p className="font-semibold text-[var(--color-text-primary)]">Shipping snapshot</p>
                  <p>{confirmation.shippingMethodName ?? "Method unavailable"}</p>
                  <p>
                    {confirmation.shippingCarrier ?? "Carrier unavailable"}
                    {confirmation.shippingService ? ` / ${confirmation.shippingService}` : ""}
                  </p>
                </div>
              )}
            </aside>

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                Payments
              </p>
              <div className="mt-4 flex flex-col gap-3">
                {confirmation.payments.length > 0 ? (
                  confirmation.payments.map((payment) => (
                    <div
                      key={payment.id}
                      className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]"
                    >
                      <p className="font-semibold text-[var(--color-text-primary)]">
                        {payment.provider} - {payment.status}
                      </p>
                      <p>{formatMoney(payment.amountMinor, payment.currency)}</p>
                      <p>Reference: {payment.providerReference ?? "Unavailable"}</p>
                      {payment.paidAtUtc ? <p>Paid: {formatDateTime(payment.paidAtUtc)}</p> : null}
                    </div>
                  ))
                ) : (
                  <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                    No payment attempts are attached yet.
                  </p>
                )}
              </div>

              {!paid && (
                <form action={createStorefrontPaymentIntentAction} className="mt-6">
                  <input type="hidden" name="orderId" value={confirmation.orderId} />
                  <input type="hidden" name="orderNumber" value={confirmation.orderNumber} />
                  <button
                    type="submit"
                    className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                  >
                    Continue to payment
                  </button>
                </form>
              )}

              {paid && (
                <div className="mt-6">
                  <StatusBanner
                    title="Payment is already recorded."
                    message="No additional payment handoff is required for this order."
                  />
                </div>
              )}
            </aside>

            <div className="flex flex-wrap gap-3">
              <Link
                href="/catalog"
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                Continue shopping
              </Link>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
