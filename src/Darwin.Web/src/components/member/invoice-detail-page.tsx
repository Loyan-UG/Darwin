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
import { createMemberInvoicePaymentIntentAction } from "@/features/member-portal/actions";
import type { MemberInvoiceDetail } from "@/features/member-portal/types";
import {
  formatResource,
  getMemberResource,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { toWebApiUrl } from "@/lib/webapi-url";

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
};

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
}: InvoiceDetailPageProps) {
  const copy = getMemberResource(culture);
  const resolvedPaymentError = resolveLocalizedQueryMessage(paymentError, copy);

  if (!invoice) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <div className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.invoiceDetailUnavailableTitle}
            message={formatResource(copy.invoiceDetailUnavailableMessage, { status })}
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

  const documentUrl = toWebApiUrl(invoice.actions.documentPath);
  const paymentAttention =
    invoice.actions.canRetryPayment || invoice.balanceMinor > 0;
  const settledAtUtc = invoice.paidAtUtc ?? null;
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
            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.invoiceLinesTitle}</p>
              <div className="mt-5 flex flex-col gap-4">
                {invoice.lines.map((line) => (
                  <article key={line.id} className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
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
              <div className="mt-5 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                {invoice.paymentSummary}
              </div>
            </div>
          </div>

          <div className="flex flex-col gap-5">
            <MemberPortalNav culture={culture} activePath="/invoices" />

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.invoiceDetailReadinessTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.invoiceDetailReadinessMessage, {
                  balance: formatMoney(invoice.balanceMinor, invoice.currency, culture),
                })}
              </p>
              <div className="mt-5 grid gap-3">
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.invoiceDetailReadinessPaymentLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {paymentAttention
                      ? copy.invoiceDetailReadinessPaymentAttention
                      : copy.invoiceDetailReadinessPaymentHealthy}
                  </p>
                </article>
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.invoiceDetailReadinessBalanceLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {formatMoney(invoice.balanceMinor, invoice.currency, culture)}
                  </p>
                </article>
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.invoiceDetailReadinessDueLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {formatDateTime(invoice.dueDateUtc, culture)}
                  </p>
                </article>
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.summaryTitle}</p>
              <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
                <div className="flex items-center justify-between"><span>{copy.statusLabel}</span><span>{invoice.status}</span></div>
                <div className="flex items-center justify-between"><span>{copy.createdLabel}</span><span>{formatDateTime(invoice.createdAtUtc, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.dueDateLabel}</span><span>{formatDateTime(invoice.dueDateUtc, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.netLabel}</span><span>{formatMoney(invoice.totalNetMinor, invoice.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.taxLabel}</span><span>{formatMoney(invoice.totalTaxMinor, invoice.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.settledLabel}</span><span>{formatMoney(invoice.settledAmountMinor, invoice.currency, culture)}</span></div>
                <div className="flex items-center justify-between"><span>{copy.balanceOnlyLabel}</span><span>{formatMoney(invoice.balanceMinor, invoice.currency, culture)}</span></div>
                <div className="flex items-center justify-between border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]"><span>{copy.totalLabel}</span><span>{formatMoney(invoice.totalGrossMinor, invoice.currency, culture)}</span></div>
              </div>
            </aside>

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.invoiceDetailTimelineTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.invoiceDetailTimelineMessage}
              </p>
              <div className="mt-5 flex flex-col gap-3">
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.invoiceDetailTimelineCreatedLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {formatDateTime(invoice.createdAtUtc, culture)}
                  </p>
                </article>
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.invoiceDetailTimelineDueLabel}
                  </p>
                  <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {formatDateTime(invoice.dueDateUtc, culture)}
                  </p>
                </article>
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.invoiceDetailStorefrontWindowTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.invoiceDetailStorefrontWindowMessage, {
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
                      {copy.invoiceDetailStorefrontCmsTitle}
                    </p>
                    <Link
                      href={localizeHref("/cms", culture)}
                      className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                    >
                      {copy.invoiceDetailStorefrontCmsCta}
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
                            {page.metaDescription ?? copy.invoiceDetailStorefrontCmsFallbackDescription}
                          </p>
                        </Link>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.invoiceDetailStorefrontCmsEmptyMessage, {
                        status: cmsPagesStatus,
                      })}
                    </p>
                  )}
                </article>
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {copy.invoiceDetailStorefrontCatalogTitle}
                    </p>
                    <Link
                      href={localizeHref("/catalog", culture)}
                      className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                    >
                      {copy.invoiceDetailStorefrontCatalogCta}
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
                            {category.description ?? copy.invoiceDetailStorefrontCatalogFallbackDescription}
                          </p>
                        </Link>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.invoiceDetailStorefrontCatalogEmptyMessage, {
                        status: categoriesStatus,
                      })}
                    </p>
                  )}
                </article>
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {copy.invoiceDetailStorefrontProductTitle}
                    </p>
                    <Link
                      href={localizeHref("/catalog", culture)}
                      className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                    >
                      {copy.invoiceDetailStorefrontProductCta}
                    </Link>
                  </div>
                  {rankedProducts.length > 0 ? (
                    <div className="mt-4 flex flex-col gap-3">
                      <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                        {cartLinkedSlugSet.size > 0
                          ? copy.invoiceDetailStorefrontProductCartAwareMessage
                          : copy.invoiceDetailStorefrontProductMessage}
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
                                    copy.invoiceDetailStorefrontProductOfferDescription,
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
                                  copy.invoiceDetailStorefrontProductFallbackDescription}
                            </p>
                            <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                              {formatMoney(product.priceMinor, product.currency, culture)}
                            </p>
                            {savingsPercent !== null ? (
                              <p className="mt-2 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                                {formatResource(
                                  copy.invoiceDetailStorefrontProductOfferMeta,
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
                      {formatResource(copy.invoiceDetailStorefrontProductEmptyMessage, {
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
                {invoice.actions.canRetryPayment && (
                  <form action={createMemberInvoicePaymentIntentAction}>
                    <input type="hidden" name="invoiceId" value={invoice.id} />
                    <input type="hidden" name="failurePath" value={localizeHref(`/invoices/${invoice.id}`, culture)} />
                    <button type="submit" className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
                      {copy.retryPaymentCta}
                    </button>
                  </form>
                )}
                {invoice.orderId ? (
                  <Link href={localizeHref(`/orders/${invoice.orderId}`, culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                    {copy.openLinkedOrderCta}
                  </Link>
                ) : null}
                {documentUrl ? (
                  <a href={documentUrl} target="_blank" rel="noopener noreferrer" className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
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

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
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
