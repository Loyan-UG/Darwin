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
import { createMemberInvoicePaymentIntentAction } from "@/features/member-portal/actions";
import type { MemberInvoiceDetail } from "@/features/member-portal/types";
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
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { buildInvoicePath, buildOrderPath } from "@/lib/entity-paths";
import { localizeHref } from "@/lib/locale-routing";
import { getSafeExternalLinkProps, toWebApiUrl } from "@/lib/webapi-url";

type InvoiceDetailPageProps = {
  culture: string;
  invoice: MemberInvoiceDetail | null;
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
}: InvoiceDetailPageProps) {
  const copy = getMemberResource(culture);
  const statusLabel = resolveApiStatusLabel(status, copy) ?? status;
  const cmsPagesStatusLabel = resolveApiStatusLabel(cmsPagesStatus, copy) ?? cmsPagesStatus;
  const categoriesStatusLabel =
    resolveApiStatusLabel(categoriesStatus, copy) ?? categoriesStatus;
  const productsStatusLabel = resolveApiStatusLabel(productsStatus, copy) ?? productsStatus;
  const storefrontCartStatusLabel =
    resolveApiStatusLabel(storefrontCartStatus, copy) ?? storefrontCartStatus;
  const resolvedPaymentError = resolveLocalizedQueryMessage(paymentError, copy);

  if (!invoice) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <div className="w-full rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.invoiceDetailUnavailableTitle}
            message={formatResource(copy.invoiceDetailUnavailableMessage, {
              status: statusLabel,
            })}
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
              includeOrders
            />
          </div>
        </div>
      </section>
    );
  }

  const documentPath = invoice.actions.documentPath
    ? `${invoice.actions.documentPath}?culture=${encodeURIComponent(culture)}`
    : null;
  const documentUrl = documentPath ? toWebApiUrl(documentPath) : "";
  const paymentAttention =
    invoice.actions.canRetryPayment || invoice.balanceMinor > 0;
  const settledAtUtc = invoice.paidAtUtc ?? null;
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
      formatResource(copy.invoiceDetailStorefrontProductOfferDescription, {
        savingsPercent: input.savingsPercent,
        price: input.price,
      }),
    describeWithoutSavings: (product) =>
      product.shortDescription ?? copy.invoiceDetailStorefrontProductFallbackDescription,
    fallbackDescription: copy.invoiceDetailStorefrontProductFallbackDescription,
    formatMeta: (product) =>
      typeof product.compareAtPriceMinor === "number" &&
      product.compareAtPriceMinor > product.priceMinor
        ? formatResource(copy.invoiceDetailStorefrontProductOfferMeta, {
            compareAt: formatMoney(
              product.compareAtPriceMinor,
              product.currency,
              culture,
            ),
          })
        : null,
  });
  const cmsSpotlightCards = buildStorefrontPageSpotlightCards(cmsPages, {
    prefix: "invoice-detail",
    fallbackDescription: copy.invoiceDetailStorefrontCmsFallbackDescription,
  });
  const categorySpotlightCards = buildStorefrontCategorySpotlightLinkCards(categories, {
    prefix: "invoice-detail",
    fallbackDescription: copy.invoiceDetailStorefrontCatalogFallbackDescription,
  });
  const promotionLaneCards = buildMemberPromotionLaneCards(rankedProducts, culture);
  const safeExternalLinkProps = getSafeExternalLinkProps();
  const sectionLinks = [
    { href: "#invoice-detail-overview", label: copy.invoiceDetailEyebrow },
    { href: "#invoice-detail-lines", label: copy.invoiceLinesTitle },
    { href: "#invoice-detail-readiness", label: copy.invoiceDetailReadinessTitle },
    { href: "#invoice-detail-storefront", label: copy.invoiceDetailStorefrontWindowTitle },
    { href: "#invoice-detail-actions", label: copy.actionsTitle },
  ];

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <SurfaceSectionNav items={sectionLinks} />
        <div id="invoice-detail-overview" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
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
            <Link href={localizeHref("/invoices", culture)} className="transition hover:text-[var(--color-text-primary)]">
              {copy.memberBreadcrumbInvoices}
            </Link>
            <span>/</span>
            <span className="text-[var(--color-text-primary)]">{invoice.orderNumber ?? invoice.id}</span>
          </nav>
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">{copy.invoiceDetailEyebrow}</p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {invoice.orderNumber ?? invoice.id}
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
            <div id="invoice-detail-lines" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.invoiceLinesTitle}</p>
              <div className="mt-5 flex flex-col gap-4">
                {invoice.lines.map((line) => (
                  <article key={line.id} className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <div className="flex flex-wrap items-start justify-between gap-4">
                      <div>
                        <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">{line.description}</h2>
                        <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                          {formatResource(copy.qtyTaxRateLabel, {
                            quantity: line.quantity,
                            taxRate: line.taxRate,
                          })}
                        </p>
                      </div>
                      <p className="text-sm font-semibold text-[var(--color-text-primary)]">{formatMoney(line.totalGrossMinor, invoice.currency, culture)}</p>
                    </div>
                  </article>
                ))}
              </div>
            </div>

            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.paymentSummaryTitle}</p>
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
            </div>
          </div>

          <div className="flex flex-col gap-5">
            <MemberPortalNav culture={culture} activePath="/invoices" />

            <aside id="invoice-detail-readiness" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.invoiceDetailReadinessTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.invoiceDetailReadinessMessage, {
                  balance: formatMoney(invoice.balanceMinor, invoice.currency, culture),
                })}
              </p>
              <div className="mt-5 grid gap-3">
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.invoiceDetailReadinessPaymentLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {paymentAttention
                      ? copy.invoiceDetailReadinessPaymentAttention
                      : copy.invoiceDetailReadinessPaymentHealthy}
                  </p>
                </article>
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.invoiceDetailReadinessBalanceLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {formatMoney(invoice.balanceMinor, invoice.currency, culture)}
                  </p>
                </article>
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.invoiceDetailReadinessDueLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {formatDateTime(invoice.dueDateUtc, culture)}
                  </p>
                </article>
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.invoiceDetailReadinessDocumentLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {documentUrl
                      ? copy.invoiceDetailReadinessDocumentReady
                      : copy.invoiceDetailReadinessDocumentMissing}
                  </p>
                </article>
              </div>
            </aside>

            <AccountContentCompositionWindow
              culture={culture}
              routeCard={{
                label: copy.accountCompositionJourneyCurrentLabel,
                title: invoice.orderNumber ?? invoice.id,
                description: formatResource(copy.invoiceDetailReadinessMessage, {
                  balance: formatMoney(invoice.balanceMinor, invoice.currency, culture),
                }),
    href: buildInvoicePath(invoice.id),
                ctaLabel: copy.accountCompositionJourneyCurrentCta,
              }}
              nextCard={{
                label: copy.accountCompositionJourneyNextLabel,
                title: invoice.orderId ? copy.ordersTitle : copy.invoicesTitle,
                description: invoice.orderId
                  ? copy.invoiceDetailPortalNote
                  : copy.ordersPortalNote,
    href: invoice.orderId ? buildOrderPath(invoice.orderId) : "/orders",
                ctaLabel: invoice.orderId
                  ? copy.openLinkedOrderCta
                  : copy.accountCompositionJourneySecurityNextCta,
              }}
              routeMapItems={[
                {
                  label: copy.accountCompositionRouteMapProfileLabel,
                  title: copy.invoicesTitle,
                  description: copy.invoicesPortalNote,
                  href: "/invoices",
                  ctaLabel: copy.backToInvoicesCta,
                },
                {
                  label: copy.accountCompositionRouteMapNextLabel,
                  title: copy.invoiceDetailTimelineTitle,
                  description: copy.invoiceDetailTimelineMessage,
    href: buildInvoicePath(invoice.id),
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
                <div className="flex items-center justify-between"><span>{copy.statusLabel}</span><span>{localizeInvoiceStatus(invoice.status, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.createdLabel}</span><span>{formatDateTime(invoice.createdAtUtc, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.dueDateLabel}</span><span>{formatDateTime(invoice.dueDateUtc, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.netLabel}</span><span>{formatMoney(invoice.totalNetMinor, invoice.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.taxLabel}</span><span>{formatMoney(invoice.totalTaxMinor, invoice.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.settledLabel}</span><span>{formatMoney(invoice.settledAmountMinor, invoice.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.balanceOnlyLabel}</span><span>{formatMoney(invoice.balanceMinor, invoice.currency, culture)}</span></div>
                <div className="flex items-center justify-between border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]"><span>{copy.totalLabel}</span><span>{formatMoney(invoice.totalGrossMinor, invoice.currency, culture)}</span></div>
              </div>
            </aside>

            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.invoiceDetailTimelineTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.invoiceDetailTimelineMessage}
              </p>
              <div className="mt-5 flex flex-col gap-3">
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.invoiceDetailTimelineCreatedLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {formatDateTime(invoice.createdAtUtc, culture)}
                  </p>
                </article>
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.invoiceDetailTimelineDueLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {formatDateTime(invoice.dueDateUtc, culture)}
                  </p>
                </article>
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.invoiceDetailTimelineSettledLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {settledAtUtc
                      ? formatDateTime(settledAtUtc, culture)
                      : copy.timelineUnavailable}
                  </p>
                </article>
              </div>
            </aside>

            <div id="invoice-detail-storefront" className="scroll-mt-28">
              <MemberStorefrontWindow
                culture={culture}
                title={copy.invoiceDetailStorefrontWindowTitle}
                message={formatResource(copy.invoiceDetailStorefrontWindowMessage, {
                  cmsStatus: cmsPagesStatusLabel,
                  categoriesStatus: categoriesStatusLabel,
                  productsStatus: productsStatusLabel,
                  pageCount: cmsPages.length,
                  categoryCount: categories.length,
                  productCount: products.length,
                })}
                cmsTitle={copy.invoiceDetailStorefrontCmsTitle}
                cmsCtaLabel={copy.invoiceDetailStorefrontCmsCta}
                cmsCards={cmsSpotlightCards}
                cmsEmptyMessage={formatResource(copy.invoiceDetailStorefrontCmsEmptyMessage, {
                  status: cmsPagesStatusLabel,
                })}
                catalogTitle={copy.invoiceDetailStorefrontCatalogTitle}
                catalogCtaLabel={copy.invoiceDetailStorefrontCatalogCta}
                categoryCards={categorySpotlightCards}
                catalogEmptyMessage={formatResource(copy.invoiceDetailStorefrontCatalogEmptyMessage, {
                  status: categoriesStatusLabel,
                })}
                productTitle={copy.invoiceDetailStorefrontProductTitle}
                productCtaLabel={copy.invoiceDetailStorefrontProductCta}
                productMessage={
                  cartLinkedSlugSet.size > 0
                    ? copy.invoiceDetailStorefrontProductCartAwareMessage
                    : copy.invoiceDetailStorefrontProductMessage
                }
                productCards={storefrontOfferCards}
                productEmptyMessage={formatResource(copy.invoiceDetailStorefrontProductEmptyMessage, {
                  status: productsStatusLabel,
                })}
                promotionLaneSectionTitle={copy.memberStorefrontPromotionLaneSectionTitle}
                promotionLaneSectionMessage={copy.memberStorefrontPromotionLaneSectionMessage}
                promotionLaneCards={promotionLaneCards}
                cartSectionTitle={copy.invoiceDetailStorefrontCartTitle}
                cartSectionMessage={
                  storefrontCart && storefrontCart.items.length > 0
                    ? formatResource(copy.invoiceDetailStorefrontCartMessage, {
                        status: storefrontCartStatusLabel,
                        count: storefrontCart.items.length,
                      })
                    : formatResource(copy.invoiceDetailStorefrontCartEmptyMessage, {
                        status: storefrontCartStatusLabel,
                      })
                }
                cartSectionCartCtaLabel={copy.invoiceDetailStorefrontCartCta}
                cartSectionCheckoutCtaLabel={copy.invoiceDetailStorefrontCheckoutCta}
              />
            </div>

            <aside id="invoice-detail-actions" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">{copy.actionsTitle}</p>
              <div className="mt-4 flex flex-col gap-3">
                {invoice.actions.canRetryPayment && (
                  <form action={createMemberInvoicePaymentIntentAction}>
                    <input type="hidden" name="invoiceId" value={invoice.id} />
                    <input type="hidden" name="culture" value={culture} />
            <input type="hidden" name="failurePath" value={localizeHref(buildInvoicePath(invoice.id), culture)} />
                    <button type="submit" className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
                      {copy.retryPaymentCta}
                    </button>
                  </form>
                )}
                {invoice.orderId ? (
              <Link href={localizeHref(buildOrderPath(invoice.orderId), culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                    {copy.openLinkedOrderCta}
                  </Link>
                ) : null}
                {documentUrl ? (
                  <a href={documentUrl} {...safeExternalLinkProps} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                    {copy.downloadDocumentCta}
                  </a>
                ) : (
                  <span className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-secondary)]">
                    {copy.documentUnavailableLabel}
                  </span>
                )}
                <Link href={localizeHref("/invoices", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  {copy.backToInvoicesCta}
                </Link>
              </div>
            </aside>

            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.invoicesRouteLabel}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.invoiceDetailPortalNote}
              </p>
            </aside>

            <MemberCrossSurfaceRail
              culture={culture}
              includeAccount={false}
              includeOrders
            />
          </div>
        </div>
      </div>
    </section>
  );
}
