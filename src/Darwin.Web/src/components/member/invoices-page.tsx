import Link from "next/link";
import { AccountContentCompositionWindow } from "@/components/account/account-content-composition-window";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { MemberCrossSurfaceRail } from "@/components/member/member-cross-surface-rail";
import { buildMemberPromotionLaneCards } from "@/components/member/member-promotion-lanes";
import { MemberStorefrontWindow } from "@/components/member/member-storefront-window";
import { SurfaceSectionNav } from "@/components/layout/surface-section-nav";
import { sortProductsByOpportunity } from "@/features/catalog/merchandising";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import type { MemberInvoiceSummary } from "@/features/member-portal/types";
import {
  buildStorefrontCategorySpotlightLinkCards,
  buildStorefrontOfferCards,
  buildStorefrontPageSpotlightCards,
} from "@/features/storefront/storefront-campaigns";
import {
  formatResource,
  getMemberResource,
  resolveApiStatusLabel,
} from "@/localization";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { buildInvoicePath } from "@/lib/entity-paths";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";

type InvoicesPageProps = {
  culture: string;
  invoices: MemberInvoiceSummary[];
  status: string;
  currentPage: number;
  totalPages: number;
  visibleQuery?: string;
  visibleState: "all" | "outstanding" | "settled";
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
  const query: Record<string, string | number | undefined> = {
    page: page > 1 ? page : undefined,
    visibleQuery: options?.visibleQuery,
    visibleState:
      options?.visibleState && options.visibleState !== "all"
        ? options.visibleState
        : undefined,
  };

  return buildAppQueryPath("/invoices", query);
}

export function InvoicesPage({
  culture,
  invoices,
  status,
  currentPage,
  totalPages,
  visibleQuery,
  visibleState,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  cartLinkedProductSlugs,
  storefrontCart,
  storefrontCartStatus,
}: InvoicesPageProps) {
  const copy = getMemberResource(culture);
  const statusLabel = resolveApiStatusLabel(status, copy) ?? status;
  const cmsPagesStatusLabel = resolveApiStatusLabel(cmsPagesStatus, copy) ?? cmsPagesStatus;
  const categoriesStatusLabel =
    resolveApiStatusLabel(categoriesStatus, copy) ?? categoriesStatus;
  const productsStatusLabel = resolveApiStatusLabel(productsStatus, copy) ?? productsStatus;
  const storefrontCartStatusLabel =
    resolveApiStatusLabel(storefrontCartStatus, copy) ?? storefrontCartStatus;
  const outstandingInvoices = invoices.filter(isOutstandingInvoice);
  const outstandingBalanceMinor = outstandingInvoices.reduce(
    (total, invoice) => total + invoice.balanceMinor,
    0,
  );
  const primaryCurrency = invoices[0]?.currency ?? "EUR";
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
  const cartLinkedSlugSet = new Set(cartLinkedProductSlugs);
  const rankedProducts =
    (cartLinkedSlugSet.size > 0
      ? sortProductsByOpportunity(
          products.filter((product) => !cartLinkedSlugSet.has(product.slug)),
        )
      : sortProductsByOpportunity(products)
    ).slice(0, 3);
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
      formatResource(copy.invoicesStorefrontProductOfferDescription, {
        savingsPercent: input.savingsPercent,
        price: input.price,
      }),
    describeWithoutSavings: (product) =>
      product.shortDescription ?? copy.invoicesStorefrontProductFallbackDescription,
    fallbackDescription: copy.invoicesStorefrontProductFallbackDescription,
    formatMeta: (product) =>
      typeof product.compareAtPriceMinor === "number" &&
      product.compareAtPriceMinor > product.priceMinor
        ? formatResource(copy.invoicesStorefrontProductOfferMeta, {
            compareAt: formatMoney(
              product.compareAtPriceMinor,
              product.currency,
              culture,
            ),
          })
        : null,
  });
  const cmsSpotlightCards = buildStorefrontPageSpotlightCards(cmsPages, {
    prefix: "invoices",
    fallbackDescription: copy.invoicesStorefrontCmsFallbackDescription,
  });
  const categorySpotlightCards = buildStorefrontCategorySpotlightLinkCards(categories, {
    prefix: "invoices",
    fallbackDescription: copy.invoicesStorefrontCatalogFallbackDescription,
  });
  const promotionLaneCards = buildMemberPromotionLaneCards(rankedProducts, culture);
  const sectionLinks = [
    { href: "#invoices-overview", label: copy.invoicesTitle },
    { href: "#invoices-filters", label: copy.invoicesFilterTitle },
    { href: "#invoices-readiness", label: copy.invoicesReadinessTitle },
    { href: "#invoices-storefront", label: copy.invoicesStorefrontWindowTitle },
    { href: "#invoices-results", label: copy.openInvoiceCta },
  ];

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <SurfaceSectionNav items={sectionLinks} />
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

        <div id="invoices-overview" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.invoicesEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.invoicesTitle}
          </h1>
        </div>

        {status !== "ok" && (
          <StatusBanner
            tone="warning"
            title={copy.invoicesWarningsTitle}
            message={formatResource(copy.invoicesWarningsMessage, { status: statusLabel })}
          />
        )}

        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.invoicesWindowTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.invoicesWindowMessage, {
              count: filteredInvoices.length,
              loadedCount: invoices.length,
              currentPage,
              totalPages,
              status: statusLabel,
            })}
          </p>
        </div>

        <div id="invoices-filters" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.invoicesFilterTitle}
            </p>
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.invoicesFilterMessage, {
                count: filteredInvoices.length,
                loadedCount: invoices.length,
              })}
            </p>
          </div>
          <form action={localizeHref("/invoices", culture)} method="get" className="mt-5 grid gap-4 lg:grid-cols-[minmax(0,1fr)_220px_auto]">
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
              {(visibleQuery || visibleState !== "all") && (
                <Link
                  href={localizeHref("/invoices", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.clearFilterCta}
                </Link>
              )}
            </div>
          </form>
        </div>

        <div id="invoices-readiness" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.invoicesReadinessTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.invoicesReadinessMessage, {
              count: outstandingInvoices.length,
              balance: formatMoney(outstandingBalanceMinor, primaryCurrency, culture),
            })}
          </p>
          <div className="mt-5 grid gap-3 sm:grid-cols-3">
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                {copy.invoicesReadinessVisibleLabel}
              </p>
              <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {invoices.length}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                {copy.invoicesReadinessOutstandingLabel}
              </p>
              <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {outstandingInvoices.length}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                {copy.invoicesReadinessBalanceLabel}
              </p>
              <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {formatMoney(outstandingBalanceMinor, primaryCurrency, culture)}
              </p>
            </div>
          </div>
          <div className="mt-5 flex flex-wrap gap-3">
            {outstandingInvoices[0] ? (
              <Link
            href={localizeHref(buildInvoicePath(outstandingInvoices[0].id), culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.invoicesReadinessPrimaryCta}
              </Link>
            ) : null}
            <Link
              href={localizeHref("/account", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.securityBackToDashboardCta}
            </Link>
          </div>
        </div>

        <AccountContentCompositionWindow
          culture={culture}
          routeCard={{
            label: copy.accountCompositionJourneyCurrentLabel,
            title: copy.invoicesTitle,
            description: formatResource(copy.invoicesWindowMessage, {
              count: filteredInvoices.length,
              loadedCount: invoices.length,
              currentPage,
              totalPages,
              status: statusLabel,
            }),
            href: buildInvoicesHref(currentPage, { visibleQuery, visibleState }),
            ctaLabel: copy.accountCompositionJourneyCurrentCta,
          }}
          nextCard={{
            label: copy.accountCompositionJourneyNextLabel,
            title: outstandingInvoices[0]?.orderNumber ?? copy.ordersTitle,
            description: outstandingInvoices[0]
              ? formatResource(copy.invoicesReadinessMessage, {
                  count: outstandingInvoices.length,
                  balance: formatMoney(outstandingBalanceMinor, primaryCurrency, culture),
                })
              : copy.ordersPortalNote,
    href: outstandingInvoices[0] ? buildInvoicePath(outstandingInvoices[0].id) : "/orders",
            ctaLabel: outstandingInvoices[0]
              ? copy.invoicesReadinessPrimaryCta
              : copy.accountCompositionJourneySecurityNextCta,
          }}
          routeMapItems={[
            {
              label: copy.accountCompositionRouteMapProfileLabel,
              title: copy.invoicesRouteLabel,
              description: copy.invoicesPortalNote,
              href: "/invoices",
              ctaLabel: copy.accountCompositionRouteMapProfileCta,
            },
            {
              label: copy.accountCompositionRouteMapNextLabel,
              title: copy.ordersTitle,
              description: copy.ordersPortalNote,
              href: "/orders",
              ctaLabel: copy.accountCompositionRouteMapAddressesCta,
            },
          ]}
          cmsPages={cmsPages}
          categories={categories}
          products={products}
        />

        <div id="invoices-storefront" className="scroll-mt-28">
          <MemberStorefrontWindow
            culture={culture}
            title={copy.invoicesStorefrontWindowTitle}
            message={formatResource(copy.invoicesStorefrontWindowMessage, {
              cmsStatus: cmsPagesStatusLabel,
              categoriesStatus: categoriesStatusLabel,
              productsStatus: productsStatusLabel,
              pageCount: cmsPages.length,
              categoryCount: categories.length,
              productCount: products.length,
            })}
            cmsTitle={copy.invoicesStorefrontCmsTitle}
            cmsCtaLabel={copy.invoicesStorefrontCmsCta}
            cmsCards={cmsSpotlightCards}
            cmsEmptyMessage={formatResource(copy.invoicesStorefrontCmsEmptyMessage, {
              status: cmsPagesStatusLabel,
            })}
            catalogTitle={copy.invoicesStorefrontCatalogTitle}
            catalogCtaLabel={copy.invoicesStorefrontCatalogCta}
            categoryCards={categorySpotlightCards}
            catalogEmptyMessage={formatResource(copy.invoicesStorefrontCatalogEmptyMessage, {
              status: categoriesStatusLabel,
            })}
            productTitle={copy.invoicesStorefrontProductTitle}
            productCtaLabel={copy.invoicesStorefrontProductCta}
            productMessage={
              cartLinkedSlugSet.size > 0
                ? formatResource(copy.invoicesStorefrontProductCartAwareMessage, {
                    count: cartLinkedSlugSet.size,
                  })
                : copy.invoicesStorefrontProductMessage
            }
            productCards={storefrontOfferCards}
            productEmptyMessage={formatResource(copy.invoicesStorefrontProductEmptyMessage, {
              status: productsStatusLabel,
            })}
            promotionLaneSectionTitle={copy.memberStorefrontPromotionLaneSectionTitle}
            promotionLaneSectionMessage={copy.memberStorefrontPromotionLaneSectionMessage}
            promotionLaneCards={promotionLaneCards}
            cartSectionTitle={copy.invoicesStorefrontCartTitle}
            cartSectionMessage={
              storefrontCart && storefrontCart.items.length > 0
                ? formatResource(copy.invoicesStorefrontCartMessage, {
                    status: storefrontCartStatusLabel,
                    count: storefrontCart.items.length,
                  })
                : formatResource(copy.invoicesStorefrontCartEmptyMessage, {
                    status: storefrontCartStatusLabel,
                  })
            }
            cartSectionCartCtaLabel={copy.invoicesStorefrontCartCta}
            cartSectionCheckoutCtaLabel={copy.invoicesStorefrontCheckoutCta}
          />
        </div>

        <div id="invoices-results" className="scroll-mt-28 grid gap-5">
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
                <Link href={localizeHref(buildInvoicePath(invoice.id), culture)} className="transition hover:text-[var(--color-brand)]">
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
        </div>

        {invoices.length === 0 && (
          <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center">
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.noInvoicesMessage}
            </p>
            <div className="mt-8 text-left">
              <MemberCrossSurfaceRail
                culture={culture}
                includeOrders
                includeInvoices={false}
                includeLoyalty
              />
            </div>
          </div>
        )}

        {invoices.length > 0 && filteredInvoices.length === 0 && (
          <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center">
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.invoicesFilterEmptyMessage}
            </p>
          </div>
        )}

        {totalPages > 1 && (
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
        )}

        </div>

        <div className="flex flex-col gap-6">
          <MemberPortalNav culture={culture} activePath="/invoices" />

          <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.invoicesRouteLabel}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.invoicesPortalNote}
            </p>
          </aside>

          <MemberCrossSurfaceRail
            culture={culture}
            includeOrders
            includeInvoices={false}
            includeLoyalty
          />
        </div>
      </div>
      </div>
    </section>
  );
}
