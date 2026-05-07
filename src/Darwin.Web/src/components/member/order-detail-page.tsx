import Link from "next/link";
import { AccountContentCompositionWindow } from "@/components/account/account-content-composition-window";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { MemberCrossSurfaceRail } from "@/components/member/member-cross-surface-rail";
import { buildMemberPromotionLaneCards } from "@/components/member/member-promotion-lanes";
import { MemberStorefrontWindow } from "@/components/member/member-storefront-window";
import { SurfaceSectionNav } from "@/components/layout/surface-section-nav";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import { sortProductsByOpportunity } from "@/features/catalog/merchandising";
import { createMemberOrderPaymentIntentAction } from "@/features/member-portal/actions";
import type { MemberOrderDetail } from "@/features/member-portal/types";
import {
  buildStorefrontCategorySpotlightLinkCards,
  buildStorefrontOfferCards,
  buildStorefrontPageSpotlightCards,
} from "@/features/storefront/storefront-campaigns";
import {
  formatResource,
  getMemberResource,
  resolveApiStatusLabel,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { parseAddressJson, type ParsedAddress } from "@/lib/address-json";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { buildInvoicePath, buildOrderPath } from "@/lib/entity-paths";
import { buildLocalizedQueryHref, localizeHref } from "@/lib/locale-routing";
import { getSafeExternalLinkProps, toWebApiUrl } from "@/lib/webapi-url";

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
  storefrontCart: import("@/features/cart/types").PublicCartSummary | null;
  storefrontCartStatus: string;
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

function getLatestUtc(values: Array<string | null | undefined>) {
  return values
    .filter((value): value is string => Boolean(value))
    .sort((left, right) => Date.parse(right) - Date.parse(left))[0];
}

function getLatestPaymentAttempt(
  payments: MemberOrderDetail["payments"],
) {
  return [...payments].sort((left, right) => {
    const leftTimestamp = Date.parse(left.createdAtUtc);
    const rightTimestamp = Date.parse(right.createdAtUtc);

    if (!Number.isNaN(leftTimestamp) && !Number.isNaN(rightTimestamp)) {
      return rightTimestamp - leftTimestamp;
    }

    return right.id.localeCompare(left.id);
  })[0];
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
  status,
  paymentError,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  cartLinkedProductSlugs,
  storefrontCart,
  storefrontCartStatus,
}: OrderDetailPageProps) {
  const copy = getMemberResource(culture);
  const statusLabel = resolveApiStatusLabel(status, copy) ?? status;
  const cmsPagesStatusLabel = resolveApiStatusLabel(cmsPagesStatus, copy) ?? cmsPagesStatus;
  const categoriesStatusLabel =
    resolveApiStatusLabel(categoriesStatus, copy) ?? categoriesStatus;
  const productsStatusLabel = resolveApiStatusLabel(productsStatus, copy) ?? productsStatus;
  const storefrontCartStatusLabel =
    resolveApiStatusLabel(storefrontCartStatus, copy) ?? storefrontCartStatus;
  const resolvedPaymentError = resolveLocalizedQueryMessage(paymentError, copy);

  if (!order) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <div className="w-full rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.orderDetailUnavailableTitle}
            message={formatResource(copy.orderDetailUnavailableMessage, {
              status: statusLabel,
            })}
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

  const documentPath = order.actions.documentPath
    ? `${order.actions.documentPath}?culture=${encodeURIComponent(culture)}`
    : null;
  const documentUrl = documentPath ? toWebApiUrl(documentPath) : "";
  const shipmentsInFlight = order.shipments.filter(
    (shipment) => !shipment.deliveredAtUtc,
  ).length;
  const retryNeeded =
    order.actions.canRetryPayment ||
    order.payments.some((payment) => payment.status.toLowerCase().includes("failed"));
  const linkedInvoiceAttention = order.invoices.filter(
    (invoice) => !invoice.paidAtUtc,
  ).length;
  const latestPaymentAttempt = getLatestPaymentAttempt(order.payments);
  const latestPaidAtUtc = getLatestUtc(order.payments.map((payment) => payment.paidAtUtc));
  const latestShippedAtUtc = getLatestUtc(
    order.shipments.map((shipment) => shipment.shippedAtUtc),
  );
  const latestDeliveredAtUtc = getLatestUtc(
    order.shipments.map((shipment) => shipment.deliveredAtUtc),
  );
  const latestInvoiceDueAtUtc = getLatestUtc(
    order.invoices.map((invoice) => invoice.dueDateUtc),
  );
  const latestInvoicePaidAtUtc = getLatestUtc(
    order.invoices.map((invoice) => invoice.paidAtUtc),
  );
  const cartLinkedSlugSet = new Set(
    cartLinkedProductSlugs.map((slug) => slug.toLowerCase()),
  );
  const rankedProducts = sortProductsByOpportunity(products)
    .filter((product) => !cartLinkedSlugSet.has(product.slug.toLowerCase()))
    .slice(0, 3);
  const storefrontOfferCards = buildStorefrontOfferCards(rankedProducts, {
    labels: {
      heroOffer: copy.offerCampaignHeroLabel,
      valueOffer: copy.offerCampaignValueLabel,
      priceDrop: copy.offerCampaignPriceDropLabel,
      steadyPick: copy.offerCampaignSteadyLabel,
    },
    formatPrice: (product) =>
      formatMoney(product.priceMinor, product.currency, culture),
    describeWithSavings: (_, input) =>
      formatResource(copy.orderDetailStorefrontProductOfferDescription, {
        savingsPercent: input.savingsPercent,
        price: input.price,
      }),
    describeWithoutSavings: (product) =>
      product.shortDescription ?? copy.orderDetailStorefrontProductFallbackDescription,
    fallbackDescription: copy.orderDetailStorefrontProductFallbackDescription,
    formatMeta: (product) =>
      typeof product.compareAtPriceMinor === "number" &&
      product.compareAtPriceMinor > product.priceMinor
        ? formatResource(copy.orderDetailStorefrontProductOfferMeta, {
            compareAt: formatMoney(
              product.compareAtPriceMinor,
              product.currency,
              culture,
            ),
          })
        : null,
  });
  const cmsSpotlightCards = buildStorefrontPageSpotlightCards(cmsPages, {
    prefix: "order-detail",
    fallbackDescription: copy.orderDetailStorefrontCmsFallbackDescription,
  });
  const categorySpotlightCards = buildStorefrontCategorySpotlightLinkCards(categories, {
    prefix: "order-detail",
    fallbackDescription: copy.orderDetailStorefrontCatalogFallbackDescription,
  });
  const promotionLaneCards = buildMemberPromotionLaneCards(rankedProducts, culture);
  const safeExternalLinkProps = getSafeExternalLinkProps();
  const sectionLinks = [
    { href: "#order-detail-overview", label: copy.orderDetailEyebrow },
    { href: "#order-detail-lines", label: copy.orderLinesTitle },
    { href: "#order-detail-readiness", label: copy.orderDetailReadinessTitle },
    { href: "#order-detail-storefront", label: copy.orderDetailStorefrontWindowTitle },
    { href: "#order-detail-actions", label: copy.actionsTitle },
  ];

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <SurfaceSectionNav items={sectionLinks} />
        <div id="order-detail-overview" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
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
            <div id="order-detail-lines" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
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

            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.orderLinesTitle}</p>
              <div className="mt-5 flex flex-col gap-4">
                {order.lines.map((line) => (
                  <article key={line.id} className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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
              <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.paymentsTitle}</p>
                {order.payments.length > 0 ? (
                  <div className="mt-5 flex flex-col gap-4">
                    {order.payments.map((payment) => (
                      <article key={payment.id} className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                        <div className="flex flex-wrap items-start justify-between gap-4">
                          <div>
                            <p className="text-sm font-semibold text-[var(--color-text-primary)]">{payment.provider}</p>
                            <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">{localizePaymentStatus(payment.status, culture)}</p>
                            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                              {copy.createdLabel} {formatDateTime(payment.createdAtUtc, culture)}
                            </p>
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

              <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.shipmentsTitle}</p>
                {order.shipments.length > 0 ? (
                  <div className="mt-5 flex flex-col gap-4">
                    {order.shipments.map((shipment) => (
                      <article key={shipment.id} className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                        <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                          {shipment.carrier} / {shipment.service}
                        </p>
                        <div className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                          <p>{formatResource(copy.shipmentStatusLabel, { value: localizeShipmentStatus(shipment.status, culture) })}</p>
                          {shipment.trackingNumber ? <p>{formatResource(copy.trackingLabel, { value: shipment.trackingNumber })}</p> : null}
                          {shipment.trackingUrl ? (
                            <p>
                              <a
                                href={shipment.trackingUrl}
                                {...safeExternalLinkProps}
                                className="font-semibold text-[var(--color-brand)] underline-offset-4 transition hover:underline"
                              >
                                {copy.trackShipmentCta}
                              </a>
                            </p>
                          ) : null}
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

            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.linkedInvoicesTitle}</p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">{copy.linkedInvoicesDescription}</p>
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
                <div className="mt-5 flex flex-col gap-4">
                  {order.invoices.map((invoice) => (
                    <article key={invoice.id} className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                      <div className="flex flex-wrap items-start justify-between gap-4">
                        <div>
                          <p className="text-sm font-semibold text-[var(--color-text-primary)]">{localizeInvoiceStatus(invoice.status, culture)}</p>
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
            </div>
          </div>

          <div className="flex flex-col gap-5">
            <MemberPortalNav culture={culture} activePath="/orders" />

            <aside id="order-detail-readiness" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
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
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.orderDetailReadinessPaymentLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {retryNeeded
                      ? copy.orderDetailReadinessPaymentAttention
                      : copy.orderDetailReadinessPaymentHealthy}
                  </p>
                  {latestPaymentAttempt ? (
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {latestPaymentAttempt.provider} / {localizePaymentStatus(latestPaymentAttempt.status, culture)}
                    </p>
                  ) : null}
                </article>
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.orderDetailReadinessShipmentLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {formatResource(copy.orderDetailReadinessShipmentValue, {
                      count: shipmentsInFlight,
                    })}
                  </p>
                </article>
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.orderDetailReadinessInvoicesLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {formatResource(copy.orderDetailReadinessInvoicesValue, {
                      count: linkedInvoiceAttention,
                    })}
                  </p>
                </article>
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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

            <AccountContentCompositionWindow
              culture={culture}
              routeCard={{
                label: copy.accountCompositionJourneyCurrentLabel,
                title: order.orderNumber,
                description: formatResource(copy.orderDetailReadinessMessage, {
                  shipments: shipmentsInFlight,
                  invoices: linkedInvoiceAttention,
                }),
    href: buildOrderPath(order.id),
                ctaLabel: copy.accountCompositionJourneyCurrentCta,
              }}
              nextCard={{
                label: copy.accountCompositionJourneyNextLabel,
                title: order.invoices[0] ? copy.linkedInvoicesTitle : copy.invoicesTitle,
                description: order.invoices[0]
                  ? copy.linkedInvoicesDescription
                  : copy.invoicesPortalNote,
    href: order.invoices[0] ? buildInvoicePath(order.invoices[0].id) : "/invoices",
                ctaLabel: order.invoices[0]
                  ? copy.openInvoiceCta
                  : copy.accountCompositionJourneyAddressesCta,
              }}
              routeMapItems={[
                {
                  label: copy.accountCompositionRouteMapProfileLabel,
                  title: copy.ordersTitle,
                  description: copy.ordersPortalNote,
                  href: "/orders",
                  ctaLabel: copy.backToOrdersCta,
                },
                {
                  label: copy.accountCompositionRouteMapNextLabel,
                  title: copy.summaryTitle,
                  description: formatResource(copy.orderDetailTimelineMessage, {}),
    href: buildOrderPath(order.id),
                  ctaLabel: copy.accountCompositionRouteMapProfileCta,
                },
              ]}
              cmsPages={cmsPages}
              categories={categories}
              products={products}
            />

            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.summaryTitle}</p>
              <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
                <div className="flex items-center justify-between"><span>{copy.statusLabel}</span><span>{localizeOrderStatus(order.status, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.createdLabel}</span><span>{formatDateTime(order.createdAtUtc, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.subtotalLabel}</span><span>{formatMoney(order.subtotalNetMinor, order.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.taxLabel}</span><span>{formatMoney(order.taxTotalMinor, order.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.shippingSummaryLabel}</span><span>{formatMoney(order.shippingTotalMinor, order.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.discountLabel}</span><span>{formatMoney(order.discountTotalMinor, order.currency, culture)}</span></div>
                <div className="flex items-center justify-between border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]"><span>{copy.totalLabel}</span><span>{formatMoney(order.grandTotalGrossMinor, order.currency, culture)}</span></div>
              </div>

              {(order.shippingMethodName || order.shippingCarrier || order.shippingService) && (
                <div className="mt-6 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  <p className="font-semibold text-[var(--color-text-primary)]">{copy.shippingSnapshotTitle}</p>
                  <p>{order.shippingMethodName ?? copy.methodUnavailable}</p>
                  <p>
                    {order.shippingCarrier ?? copy.carrierUnavailable}
                    {order.shippingService ? ` / ${order.shippingService}` : ""}
                  </p>
                </div>
              )}
            </aside>

            <aside id="order-detail-actions" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.orderDetailTimelineTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.orderDetailTimelineMessage}
              </p>
              <div className="mt-5 flex flex-col gap-3">
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.orderDetailTimelineCreatedLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {formatDateTime(order.createdAtUtc, culture)}
                  </p>
                </article>
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.orderDetailTimelinePaidLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {latestPaidAtUtc
                      ? formatDateTime(latestPaidAtUtc, culture)
                      : copy.timelineUnavailable}
                  </p>
                </article>
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.orderDetailTimelineShippedLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {latestShippedAtUtc
                      ? formatDateTime(latestShippedAtUtc, culture)
                      : copy.timelineUnavailable}
                  </p>
                </article>
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.orderDetailTimelineDeliveredLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {latestDeliveredAtUtc
                      ? formatDateTime(latestDeliveredAtUtc, culture)
                      : copy.timelineUnavailable}
                  </p>
                </article>
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.orderDetailTimelineInvoiceDueLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {latestInvoiceDueAtUtc
                      ? formatDateTime(latestInvoiceDueAtUtc, culture)
                      : copy.timelineUnavailable}
                  </p>
                </article>
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.orderDetailTimelineInvoicePaidLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {latestInvoicePaidAtUtc
                      ? formatDateTime(latestInvoicePaidAtUtc, culture)
                      : copy.timelineUnavailable}
                  </p>
                </article>
              </div>
            </aside>

            <div id="order-detail-storefront" className="scroll-mt-28">
              <MemberStorefrontWindow
                culture={culture}
                title={copy.orderDetailStorefrontWindowTitle}
                message={formatResource(copy.orderDetailStorefrontWindowMessage, {
                  cmsStatus: cmsPagesStatusLabel,
                  categoriesStatus: categoriesStatusLabel,
                  productsStatus: productsStatusLabel,
                  pageCount: cmsPages.length,
                  categoryCount: categories.length,
                  productCount: products.length,
                })}
                cmsTitle={copy.orderDetailStorefrontCmsTitle}
                cmsCtaLabel={copy.orderDetailStorefrontCmsCta}
                cmsCards={cmsSpotlightCards}
                cmsEmptyMessage={formatResource(copy.orderDetailStorefrontCmsEmptyMessage, {
                  status: cmsPagesStatusLabel,
                })}
                catalogTitle={copy.orderDetailStorefrontCatalogTitle}
                catalogCtaLabel={copy.orderDetailStorefrontCatalogCta}
                categoryCards={categorySpotlightCards}
                catalogEmptyMessage={formatResource(copy.orderDetailStorefrontCatalogEmptyMessage, {
                  status: categoriesStatusLabel,
                })}
                productTitle={copy.orderDetailStorefrontProductTitle}
                productCtaLabel={copy.orderDetailStorefrontProductCta}
                productMessage={
                  cartLinkedSlugSet.size > 0
                    ? copy.orderDetailStorefrontProductCartAwareMessage
                    : copy.orderDetailStorefrontProductMessage
                }
                productCards={storefrontOfferCards}
                productEmptyMessage={formatResource(copy.orderDetailStorefrontProductEmptyMessage, {
                  status: productsStatusLabel,
                })}
                promotionLaneSectionTitle={copy.memberStorefrontPromotionLaneSectionTitle}
                promotionLaneSectionMessage={copy.memberStorefrontPromotionLaneSectionMessage}
                promotionLaneCards={promotionLaneCards}
                cartSectionTitle={copy.orderDetailStorefrontCartTitle}
                cartSectionMessage={
                  storefrontCart && storefrontCart.items.length > 0
                    ? formatResource(copy.orderDetailStorefrontCartMessage, {
                        status: storefrontCartStatusLabel,
                        count: storefrontCart.items.length,
                      })
                    : formatResource(copy.orderDetailStorefrontCartEmptyMessage, {
                        status: storefrontCartStatusLabel,
                      })
                }
                cartSectionCartCtaLabel={copy.orderDetailStorefrontCartCta}
                cartSectionCheckoutCtaLabel={copy.orderDetailStorefrontCheckoutCta}
              />
            </div>

            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">{copy.actionsTitle}</p>
              <div className="mt-4 flex flex-col gap-3">
                {order.actions.canRetryPayment && (
                  <form action={createMemberOrderPaymentIntentAction}>
                    <input type="hidden" name="orderId" value={order.id} />
                    <input type="hidden" name="culture" value={culture} />
            <input type="hidden" name="failurePath" value={localizeHref(buildOrderPath(order.id), culture)} />
                    <button type="submit" className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
                      {copy.retryPaymentCta}
                    </button>
                  </form>
                )}
                <Link href={buildLocalizedQueryHref(`/checkout/orders/${order.id}/confirmation`, { orderNumber: order.orderNumber }, culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  {copy.openConfirmationCta}
                </Link>
                {documentUrl ? (
                  <a href={documentUrl} {...safeExternalLinkProps} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
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

            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
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
