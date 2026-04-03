import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { MemberCrossSurfaceRail } from "@/components/member/member-cross-surface-rail";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  getProductSavingsPercent,
  sortProductsByOpportunity,
} from "@/features/catalog/merchandising";
import { createMemberOrderPaymentIntentAction } from "@/features/member-portal/actions";
import type { MemberOrderDetail } from "@/features/member-portal/types";
import {
  formatResource,
  getMemberResource,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { parseAddressJson, type ParsedAddress } from "@/lib/address-json";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, buildLocalizedQueryHref, localizeHref } from "@/lib/locale-routing";
import { toWebApiUrl } from "@/lib/webapi-url";

type OrderDetailPageProps = {
  culture: string;
  order: MemberOrderDetail | null;
  status: string;
  paymentError?: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
  cartLinkedProductSlugs: string[];
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
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  cartLinkedProductSlugs,
}: OrderDetailPageProps) {
  const copy = getMemberResource(culture);
  const resolvedPaymentError = resolveLocalizedQueryMessage(paymentError, copy);

  if (!order) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <div className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.orderDetailUnavailableTitle}
            message={formatResource(copy.orderDetailUnavailableMessage, { status })}
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
              {copy.memberCrossSurfaceAccountCta}
            </Link>
            <Link
              href={localizeHref("/catalog", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.memberCrossSurfaceCatalogCta}
            </Link>
          </div>
          <div className="mt-8">
            <MemberCrossSurfaceRail
              culture={culture}
              includeAccount={false}
              includeInvoices
            />
          </div>
        </div>
      </section>
    );
  }

  const documentUrl = toWebApiUrl(order.actions.documentPath);
  const shipmentsInFlight = order.shipments.filter(
    (shipment) => !shipment.deliveredAtUtc,
  ).length;
  const retryNeeded =
    order.actions.canRetryPayment ||
    order.payments.some((payment) => payment.status.toLowerCase().includes("failed"));
  const linkedInvoiceAttention = order.invoices.filter(
    (invoice) => !invoice.paidAtUtc,
  ).length;
  const cartLinkedSlugSet = new Set(
    cartLinkedProductSlugs.map((slug) => slug.toLowerCase()),
  );
  const rankedProducts = sortProductsByOpportunity(products)
    .filter((product) => !cartLinkedSlugSet.has(product.slug.toLowerCase()))
    .slice(0, 3);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <nav
            aria-label={copy.memberBreadcrumbLabel}
            className="flex flex-wrap items-center gap-2 text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-text-muted)]"
          >
            <Link href={localizeHref("/", culture)} className="transition hover:text-[var(--color-text-primary)]">
              {copy.memberBreadcrumbHome}
            </Link>
            <span>/</span>
            <Link href={localizeHref("/account", culture)} className="transition hover:text-[var(--color-text-primary)]">
              {copy.memberBreadcrumbAccount}
            </Link>
            <span>/</span>
            <Link href={localizeHref("/orders", culture)} className="transition hover:text-[var(--color-text-primary)]">
              {copy.memberBreadcrumbOrders}
            </Link>
            <span>/</span>
            <span className="text-[var(--color-text-primary)]">{order.orderNumber}</span>
          </nav>
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">{copy.orderDetailEyebrow}</p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {order.orderNumber}
          </h1>
        </div>

        {resolvedPaymentError && (
          <StatusBanner
            tone="warning"
            title={copy.paymentRetryFailedTitle}
            message={resolvedPaymentError}
          />
        )}

        <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
          <div className="flex flex-col gap-6">
            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <div className="grid gap-5 sm:grid-cols-2">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">{copy.billingTitle}</p>
              <div className="mt-3">{renderAddress(parseAddressJson(order.billingAddressJson), culture)}</div>
                </div>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">{copy.shippingTitle}</p>
              <div className="mt-3">{renderAddress(parseAddressJson(order.shippingAddressJson), culture)}</div>
                </div>
              </div>
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.orderLinesTitle}</p>
              <div className="mt-5 flex flex-col gap-4">
                {order.lines.map((line) => (
                  <article key={line.id} className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <div className="flex flex-wrap items-start justify-between gap-4">
                      <div>
                        <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">{line.name}</h2>
                        <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                          {formatResource(copy.skuQtyLabel, { sku: line.sku, quantity: line.quantity })}
                        </p>
                      </div>
                      <p className="text-sm font-semibold text-[var(--color-text-primary)]">{formatMoney(line.lineGrossMinor, order.currency, culture)}</p>
                    </div>
                  </article>
                ))}
              </div>
            </div>

            <div className="grid gap-6 xl:grid-cols-2">
              <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.paymentsTitle}</p>
                {order.payments.length > 0 ? (
                  <div className="mt-5 flex flex-col gap-4">
                    {order.payments.map((payment) => (
                      <article key={payment.id} className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                        <div className="flex flex-wrap items-start justify-between gap-4">
                          <div>
                            <p className="text-sm font-semibold text-[var(--color-text-primary)]">{payment.provider}</p>
                            <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">{payment.status}</p>
                            {payment.providerReference ? (
                              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                                {formatResource(copy.referenceLabel, { value: payment.providerReference })}
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
                      </article>
                    ))}
                  </div>
                ) : (
                  <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.noOrderPaymentsMessage}
                  </p>
                )}
              </div>

              <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.shipmentsTitle}</p>
                {order.shipments.length > 0 ? (
                  <div className="mt-5 flex flex-col gap-4">
                    {order.shipments.map((shipment) => (
                      <article key={shipment.id} className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                        <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                          {shipment.carrier} / {shipment.service}
                        </p>
                        <div className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                          <p>{formatResource(copy.shipmentStatusLabel, { value: shipment.status })}</p>
                          {shipment.trackingNumber ? <p>{formatResource(copy.trackingLabel, { value: shipment.trackingNumber })}</p> : null}
                          {shipment.shippedAtUtc ? <p>{formatResource(copy.shippedLabel, { value: formatDateTime(shipment.shippedAtUtc, culture) })}</p> : null}
                          {shipment.deliveredAtUtc ? <p>{formatResource(copy.deliveredLabel, { value: formatDateTime(shipment.deliveredAtUtc, culture) })}</p> : null}
                        </div>
                      </article>
                    ))}
                  </div>
                ) : (
                  <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.noShipmentsMessage}
                  </p>
                )}
              </div>
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.linkedInvoicesTitle}</p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">{copy.linkedInvoicesDescription}</p>
                </div>
                {documentUrl ? (
                  <a
                    href={documentUrl}
                    target="_blank"
                    rel="noopener noreferrer"
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
                <div className="mt-5 flex flex-col gap-4">
                  {order.invoices.map((invoice) => (
                    <article key={invoice.id} className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                      <div className="flex flex-wrap items-start justify-between gap-4">
                        <div>
                          <p className="text-sm font-semibold text-[var(--color-text-primary)]">{invoice.status}</p>
                          <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                            {formatResource(copy.dueLabel, { value: formatDateTime(invoice.dueDateUtc, culture) })}
                          </p>
                          {invoice.paidAtUtc ? (
                            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                              {formatResource(copy.paidLabel, { value: formatDateTime(invoice.paidAtUtc, culture) })}
                            </p>
                          ) : null}
                        </div>
                        <div className="text-right">
                          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                            {formatMoney(invoice.totalGrossMinor, invoice.currency, culture)}
                          </p>
                          <Link
                            href={localizeHref(`/invoices/${invoice.id}`, culture)}
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
            </div>
          </div>

          <div className="flex flex-col gap-5">
            <MemberPortalNav culture={culture} activePath="/orders" />

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.orderDetailReadinessTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.orderDetailReadinessMessage, {
                  shipments: shipmentsInFlight,
                  invoices: linkedInvoiceAttention,
                })}
              </p>
              <div className="mt-5 grid gap-3">
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.orderDetailReadinessPaymentLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {retryNeeded
                      ? copy.orderDetailReadinessPaymentAttention
                      : copy.orderDetailReadinessPaymentHealthy}
                  </p>
                </article>
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.orderDetailReadinessShipmentLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {formatResource(copy.orderDetailReadinessShipmentValue, {
                      count: shipmentsInFlight,
                    })}
                  </p>
                </article>
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.orderDetailReadinessInvoicesLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {formatResource(copy.orderDetailReadinessInvoicesValue, {
                      count: linkedInvoiceAttention,
                    })}
                  </p>
                </article>
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.orderDetailReadinessDocumentLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {documentUrl
                      ? copy.orderDetailReadinessDocumentReady
                      : copy.orderDetailReadinessDocumentMissing}
                  </p>
                </article>
              </div>
            </aside>

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.summaryTitle}</p>
              <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
                <div className="flex items-center justify-between"><span>{copy.statusLabel}</span><span>{order.status}</span></div>
                <div className="flex items-center justify-between"><span>{copy.createdLabel}</span><span>{formatDateTime(order.createdAtUtc, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.subtotalLabel}</span><span>{formatMoney(order.subtotalNetMinor, order.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.taxLabel}</span><span>{formatMoney(order.taxTotalMinor, order.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.shippingSummaryLabel}</span><span>{formatMoney(order.shippingTotalMinor, order.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.discountLabel}</span><span>{formatMoney(order.discountTotalMinor, order.currency, culture)}</span></div>
                <div className="flex items-center justify-between border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]"><span>{copy.totalLabel}</span><span>{formatMoney(order.grandTotalGrossMinor, order.currency, culture)}</span></div>
              </div>

              {(order.shippingMethodName || order.shippingCarrier || order.shippingService) && (
                <div className="mt-6 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  <p className="font-semibold text-[var(--color-text-primary)]">{copy.shippingSnapshotTitle}</p>
                  <p>{order.shippingMethodName ?? copy.methodUnavailable}</p>
                  <p>
                    {order.shippingCarrier ?? copy.carrierUnavailable}
                    {order.shippingService ? ` / ${order.shippingService}` : ""}
                  </p>
                </div>
              )}
            </aside>

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.orderDetailStorefrontWindowTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.orderDetailStorefrontWindowMessage, {
                  cmsStatus: cmsPagesStatus,
                  categoriesStatus,
                  productsStatus,
                  pageCount: cmsPages.length,
                  categoryCount: categories.length,
                  productCount: products.length,
                })}
              </p>
              <div className="mt-5 grid gap-3">
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {copy.orderDetailStorefrontCmsTitle}
                    </p>
                    <Link
                      href={localizeHref("/cms", culture)}
                      className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                    >
                      {copy.orderDetailStorefrontCmsCta}
                    </Link>
                  </div>
                  {cmsPages.length > 0 ? (
                    <div className="mt-4 flex flex-col gap-3">
                      {cmsPages.map((page) => (
                        <Link
                          key={page.id}
                          href={localizeHref(`/cms/${page.slug}`, culture)}
                          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                        >
                          <p className="font-semibold text-[var(--color-text-primary)]">
                            {page.title}
                          </p>
                          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                            {page.metaDescription ?? copy.orderDetailStorefrontCmsFallbackDescription}
                          </p>
                        </Link>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.orderDetailStorefrontCmsEmptyMessage, {
                        status: cmsPagesStatus,
                      })}
                    </p>
                  )}
                </article>
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {copy.orderDetailStorefrontCatalogTitle}
                    </p>
                    <Link
                      href={localizeHref("/catalog", culture)}
                      className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                    >
                      {copy.orderDetailStorefrontCatalogCta}
                    </Link>
                  </div>
                  {categories.length > 0 ? (
                    <div className="mt-4 flex flex-col gap-3">
                      {categories.map((category) => (
                        <Link
                          key={category.id}
                          href={localizeHref(
                            buildAppQueryPath("/catalog", {
                              category: category.slug,
                            }),
                            culture,
                          )}
                          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                        >
                          <p className="font-semibold text-[var(--color-text-primary)]">
                            {category.name}
                          </p>
                          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                            {category.description ?? copy.orderDetailStorefrontCatalogFallbackDescription}
                          </p>
                        </Link>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.orderDetailStorefrontCatalogEmptyMessage, {
                        status: categoriesStatus,
                      })}
                    </p>
                  )}
                </article>
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {copy.orderDetailStorefrontProductTitle}
                    </p>
                    <Link
                      href={localizeHref("/catalog", culture)}
                      className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                    >
                      {copy.orderDetailStorefrontProductCta}
                    </Link>
                  </div>
                  {rankedProducts.length > 0 ? (
                    <div className="mt-4 flex flex-col gap-3">
                      <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                        {cartLinkedSlugSet.size > 0
                          ? copy.orderDetailStorefrontProductCartAwareMessage
                          : copy.orderDetailStorefrontProductMessage}
                      </p>
                      {rankedProducts.map((product) => {
                        const savingsPercent = getProductSavingsPercent(product);

                        return (
                          <Link
                            key={product.id}
                            href={localizeHref(`/catalog/${product.slug}`, culture)}
                            className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                          >
                            <p className="font-semibold text-[var(--color-text-primary)]">
                              {product.name}
                            </p>
                            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                              {savingsPercent !== null
                                ? formatResource(
                                    copy.orderDetailStorefrontProductOfferDescription,
                                    {
                                      savingsPercent,
                                      price: formatMoney(
                                        product.priceMinor,
                                        product.currency,
                                        culture,
                                      ),
                                    },
                                  )
                                : product.shortDescription ??
                                  copy.orderDetailStorefrontProductFallbackDescription}
                            </p>
                            <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                              {formatMoney(product.priceMinor, product.currency, culture)}
                            </p>
                            {savingsPercent !== null ? (
                              <p className="mt-2 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                                {formatResource(
                                  copy.orderDetailStorefrontProductOfferMeta,
                                  {
                                    compareAt: formatMoney(
                                      product.compareAtPriceMinor ??
                                        product.priceMinor,
                                      product.currency,
                                      culture,
                                    ),
                                  },
                                )}
                              </p>
                            ) : null}
                          </Link>
                        );
                      })}
                    </div>
                  ) : (
                    <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.orderDetailStorefrontProductEmptyMessage, {
                        status: productsStatus,
                      })}
                    </p>
                  )}
                </article>
              </div>
            </aside>

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">{copy.actionsTitle}</p>
              <div className="mt-4 flex flex-col gap-3">
                {order.actions.canRetryPayment && (
                  <form action={createMemberOrderPaymentIntentAction}>
                    <input type="hidden" name="orderId" value={order.id} />
                    <input type="hidden" name="failurePath" value={localizeHref(`/orders/${order.id}`, culture)} />
                    <button type="submit" className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
                      {copy.retryPaymentCta}
                    </button>
                  </form>
                )}
                <Link href={buildLocalizedQueryHref(`/checkout/orders/${order.id}/confirmation`, { orderNumber: order.orderNumber }, culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  {copy.openConfirmationCta}
                </Link>
                {documentUrl ? (
                  <a href={documentUrl} target="_blank" rel="noopener noreferrer" className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                    {copy.downloadDocumentCta}
                  </a>
                ) : (
                  <span className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-secondary)]">
                    {copy.documentUnavailableLabel}
                  </span>
                )}
                <Link href={localizeHref("/orders", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  {copy.backToOrdersCta}
                </Link>
              </div>
            </aside>

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.ordersRouteLabel}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.orderDetailPortalNote}
              </p>
            </aside>

            <MemberCrossSurfaceRail
              culture={culture}
              includeAccount={false}
              includeInvoices
            />
          </div>
        </div>
      </div>
    </section>
  );
}
