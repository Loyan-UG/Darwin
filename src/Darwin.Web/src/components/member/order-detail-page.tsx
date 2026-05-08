import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { createMemberOrderPaymentIntentAction } from "@/features/member-portal/actions";
import type { MemberOrderDetail } from "@/features/member-portal/types";
import {
  formatResource,
  getMemberResource,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { parseAddressJson, type ParsedAddress } from "@/lib/address-json";
import { buildInvoicePath, buildOrderPath } from "@/lib/entity-paths";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { buildLocalizedQueryHref, localizeHref } from "@/lib/locale-routing";
import { getSafeExternalLinkProps, toWebApiUrl } from "@/lib/webapi-url";

type OrderDetailPageProps = {
  culture: string;
  order: MemberOrderDetail | null;
  status: string;
  paymentError?: string;
};

function renderAddress(address: ParsedAddress | null, culture: string) {
  const copy = getMemberResource(culture);

  if (!address) {
    return (
      <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
        {copy.snapshotUnavailable}
      </p>
    );
  }

  return (
    <div className="text-sm leading-7 text-[var(--color-text-secondary)]">
      <p className="font-semibold text-[var(--color-text-primary)]">
        {address.fullName ?? copy.recipientUnavailable}
      </p>
      {address.company ? <p>{address.company}</p> : null}
      <p>{address.street1}</p>
      {address.street2 ? <p>{address.street2}</p> : null}
      <p>
        {address.postalCode} {address.city}
      </p>
      {address.state ? <p>{address.state}</p> : null}
      <p>{address.countryCode}</p>
    </div>
  );
}

function localizeOrderStatus(status: string, culture: string) {
  if (culture.toLowerCase().startsWith("en")) {
    return status;
  }

  const labels: Record<string, string> = {
    Created: "Erstellt",
    Confirmed: "Bestaetigt",
    Paid: "Bezahlt",
    PartiallyShipped: "Teilweise versendet",
    Shipped: "Versendet",
    Delivered: "Zugestellt",
    Cancelled: "Storniert",
    Refunded: "Erstattet",
    PartiallyRefunded: "Teilweise erstattet",
    Completed: "Abgeschlossen",
  };

  return labels[status] ?? status;
}

function localizePaymentStatus(status: string, culture: string) {
  if (culture.toLowerCase().startsWith("en")) {
    return status;
  }

  const labels: Record<string, string> = {
    Pending: "Ausstehend",
    Authorized: "Autorisiert",
    Captured: "Erfasst",
    Completed: "Abgeschlossen",
    Failed: "Fehlgeschlagen",
    Refunded: "Erstattet",
    Voided: "Storniert",
  };

  return labels[status] ?? status;
}

function localizeShipmentStatus(status: string, culture: string) {
  if (culture.toLowerCase().startsWith("en")) {
    return status;
  }

  const labels: Record<string, string> = {
    Pending: "Ausstehend",
    Packed: "Gepackt",
    Shipped: "Versendet",
    Delivered: "Zugestellt",
    Returned: "Retourniert",
  };

  return labels[status] ?? status;
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

export function OrderDetailPage({
  culture,
  order,
  paymentError,
}: OrderDetailPageProps) {
  const copy = getMemberResource(culture);
  const resolvedPaymentError = resolveLocalizedQueryMessage(paymentError, copy);

  if (!order) {
    return (
      <section className="mx-auto flex w-full max-w-[1180px] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <div className="w-full rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.orderDetailUnavailableTitle}
            message={copy.orderDetailUnavailableCustomerMessage}
          />
          <div className="mt-8 flex flex-wrap gap-3">
            <Link
              href={localizeHref("/orders", culture)}
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {copy.backToOrdersCta}
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

  const documentPath = order.actions.documentPath
    ? `${order.actions.documentPath}?culture=${encodeURIComponent(culture)}`
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
            <Link href={localizeHref("/orders", culture)} className="transition hover:text-[var(--color-brand)]">
              {copy.memberBreadcrumbOrders}
            </Link>
            <span>/</span>
            <span className="font-medium text-[var(--color-text-primary)]">
              {order.orderNumber}
            </span>
          </nav>

          <header className="rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_48%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
              {copy.orderDetailEyebrow}
            </p>
            <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
              {order.orderNumber}
            </h1>
            <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.orderDetailPortalNote}
            </p>
          </header>

          {resolvedPaymentError ? (
            <StatusBanner
              tone="warning"
              title={copy.paymentRetryFailedTitle}
              message={resolvedPaymentError}
            />
          ) : null}

          <section className="grid gap-5 sm:grid-cols-2">
            <article className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.billingTitle}
              </p>
              <div className="mt-4">
                {renderAddress(parseAddressJson(order.billingAddressJson), culture)}
              </div>
            </article>
            <article className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.shippingTitle}
              </p>
              <div className="mt-4">
                {renderAddress(parseAddressJson(order.shippingAddressJson), culture)}
              </div>
            </article>
          </section>

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.orderLinesTitle}
            </p>
            <div className="mt-5 grid gap-4">
              {order.lines.map((line) => (
                <article
                  key={line.id}
                  className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                >
                  <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                      <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">
                        {line.name}
                      </h2>
                      <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {formatResource(copy.skuQtyLabel, {
                          sku: line.sku,
                          quantity: line.quantity,
                        })}
                      </p>
                    </div>
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {formatMoney(line.lineGrossMinor, order.currency, culture)}
                    </p>
                  </div>
                </article>
              ))}
            </div>
          </section>

          <section className="grid gap-6 xl:grid-cols-2">
            <article className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.paymentsTitle}
              </p>
              {order.payments.length > 0 ? (
                <div className="mt-5 grid gap-4">
                  {order.payments.map((payment) => (
                    <div
                      key={payment.id}
                      className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                    >
                      <div className="flex flex-wrap items-start justify-between gap-4">
                        <div>
                          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                            {payment.provider}
                          </p>
                          <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                            {localizePaymentStatus(payment.status, culture)}
                          </p>
                          <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                            {copy.createdLabel} {formatDateTime(payment.createdAtUtc, culture)}
                          </p>
                          {payment.providerReference ? (
                            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                              {formatResource(copy.referenceLabel, {
                                value: payment.providerReference,
                              })}
                            </p>
                          ) : null}
                          {payment.paidAtUtc ? (
                            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                              {formatResource(copy.paidLabel, {
                                value: formatDateTime(payment.paidAtUtc, culture),
                              })}
                            </p>
                          ) : null}
                        </div>
                        <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                          {formatMoney(payment.amountMinor, payment.currency, culture)}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.noOrderPaymentsMessage}
                </p>
              )}
            </article>

            <article className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.shipmentsTitle}
              </p>
              {order.shipments.length > 0 ? (
                <div className="mt-5 grid gap-4">
                  {order.shipments.map((shipment) => (
                    <div
                      key={shipment.id}
                      className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                    >
                      <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                        {shipment.carrier} / {shipment.service}
                      </p>
                      <div className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        <p>
                          {formatResource(copy.shipmentStatusLabel, {
                            value: localizeShipmentStatus(shipment.status, culture),
                          })}
                        </p>
                        {shipment.trackingNumber ? (
                          <p>
                            {formatResource(copy.trackingLabel, {
                              value: shipment.trackingNumber,
                            })}
                          </p>
                        ) : null}
                        {shipment.shippedAtUtc ? (
                          <p>
                            {formatResource(copy.shippedLabel, {
                              value: formatDateTime(shipment.shippedAtUtc, culture),
                            })}
                          </p>
                        ) : null}
                        {shipment.deliveredAtUtc ? (
                          <p>
                            {formatResource(copy.deliveredLabel, {
                              value: formatDateTime(shipment.deliveredAtUtc, culture),
                            })}
                          </p>
                        ) : null}
                      </div>
                      {shipment.trackingUrl ? (
                        <a
                          href={shipment.trackingUrl}
                          {...safeExternalLinkProps}
                          className="mt-4 inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                        >
                          {copy.trackShipmentCta}
                        </a>
                      ) : null}
                    </div>
                  ))}
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.noShipmentsMessage}
                </p>
              )}
            </article>
          </section>

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                  {copy.linkedInvoicesTitle}
                </p>
                <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.linkedInvoicesDescription}
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
            {order.invoices.length > 0 ? (
              <div className="mt-5 grid gap-4">
                {order.invoices.map((invoice) => (
                  <article
                    key={invoice.id}
                    className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                  >
                    <div className="flex flex-wrap items-start justify-between gap-4">
                      <div>
                        <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                          {localizeInvoiceStatus(invoice.status, culture)}
                        </p>
                        <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                          {formatResource(copy.dueLabel, {
                            value: formatDateTime(invoice.dueDateUtc, culture),
                          })}
                        </p>
                        {invoice.paidAtUtc ? (
                          <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                            {formatResource(copy.paidLabel, {
                              value: formatDateTime(invoice.paidAtUtc, culture),
                            })}
                          </p>
                        ) : null}
                      </div>
                      <div className="text-right">
                        <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                          {formatMoney(invoice.totalGrossMinor, invoice.currency, culture)}
                        </p>
                        <Link
                          href={localizeHref(buildInvoicePath(invoice.id), culture)}
                          className="mt-3 inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                        >
                          {copy.openInvoiceCta}
                        </Link>
                      </div>
                    </div>
                  </article>
                ))}
              </div>
            ) : (
              <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.noLinkedInvoicesMessage}
              </p>
            )}
          </section>
        </div>

        <aside className="flex flex-col gap-5">
          <MemberPortalNav culture={culture} activePath="/orders" />

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.summaryTitle}
            </p>
            <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
              <div className="flex items-center justify-between gap-4">
                <span>{copy.statusLabel}</span>
                <span>{localizeOrderStatus(order.status, culture)}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span>{copy.createdLabel}</span>
                <span>{formatDateTime(order.createdAtUtc, culture)}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span>{copy.subtotalLabel}</span>
                <span>{formatMoney(order.subtotalNetMinor, order.currency, culture)}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span>{copy.taxLabel}</span>
                <span>{formatMoney(order.taxTotalMinor, order.currency, culture)}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span>{copy.shippingSummaryLabel}</span>
                <span>{formatMoney(order.shippingTotalMinor, order.currency, culture)}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span>{copy.discountLabel}</span>
                <span>{formatMoney(order.discountTotalMinor, order.currency, culture)}</span>
              </div>
              <div className="flex items-center justify-between gap-4 border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]">
                <span>{copy.totalLabel}</span>
                <span>{formatMoney(order.grandTotalGrossMinor, order.currency, culture)}</span>
              </div>
            </div>

            {(order.shippingMethodName || order.shippingCarrier || order.shippingService) ? (
              <div className="mt-6 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                <p className="font-semibold text-[var(--color-text-primary)]">
                  {copy.shippingSnapshotTitle}
                </p>
                <p>{order.shippingMethodName ?? copy.methodUnavailable}</p>
                <p>
                  {order.shippingCarrier ?? copy.carrierUnavailable}
                  {order.shippingService ? ` / ${order.shippingService}` : ""}
                </p>
              </div>
            ) : null}
          </section>

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.actionsTitle}
            </p>
            <div className="mt-4 flex flex-col gap-3">
              {order.actions.canRetryPayment ? (
                <form action={createMemberOrderPaymentIntentAction}>
                  <input type="hidden" name="orderId" value={order.id} />
                  <input type="hidden" name="culture" value={culture} />
                  <input
                    type="hidden"
                    name="failurePath"
                    value={localizeHref(buildOrderPath(order.id), culture)}
                  />
                  <button
                    type="submit"
                    className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                  >
                    {copy.retryPaymentCta}
                  </button>
                </form>
              ) : null}
              <Link
                href={buildLocalizedQueryHref(
                  "/checkout/orders/{id}/confirmation".replace("{id}", order.id),
                  { orderNumber: order.orderNumber },
                  culture,
                )}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.openConfirmationCta}
              </Link>
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
                href={localizeHref("/orders", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.backToOrdersCta}
              </Link>
            </div>
          </section>
        </aside>
      </div>
    </section>
  );
}
