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
import type { MemberOrderSummary } from "@/features/member-portal/types";
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
import { buildOrderPath } from "@/lib/entity-paths";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";

type OrdersPageProps = {
  culture: string;
  orders: MemberOrderSummary[];
  status: string;
  currentPage: number;
  totalPages: number;
  visibleQuery?: string;
  visibleState: "all" | "attention" | "settled";
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

function isAttentionOrder(order: MemberOrderSummary) {
  return /(pending|processing|payment|review|hold|open)/i.test(order.status);
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

function buildOrdersHref(
  page = 1,
  options?: {
    visibleQuery?: string;
    visibleState?: "all" | "attention" | "settled";
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

  return buildAppQueryPath("/orders", query);
}

export function OrdersPage({
  culture,
  orders,
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
}: OrdersPageProps) {
  const copy = getMemberResource(culture);
  const statusLabel = resolveApiStatusLabel(status, copy) ?? status;
  const cmsPagesStatusLabel = resolveApiStatusLabel(cmsPagesStatus, copy) ?? cmsPagesStatus;
  const categoriesStatusLabel =
    resolveApiStatusLabel(categoriesStatus, copy) ?? categoriesStatus;
  const productsStatusLabel = resolveApiStatusLabel(productsStatus, copy) ?? productsStatus;
  const storefrontCartStatusLabel =
    resolveApiStatusLabel(storefrontCartStatus, copy) ?? storefrontCartStatus;
  const attentionOrders = orders.filter(isAttentionOrder);
  const primaryCurrency = orders[0]?.currency ?? "EUR";
  const attentionGrossMinor = attentionOrders.reduce(
    (total, order) => total + order.grandTotalGrossMinor,
    0,
  );
  const normalizedVisibleQuery = visibleQuery?.trim().toLowerCase() ?? "";
  const filteredOrders = orders.filter((order) => {
    const matchesQuery =
      normalizedVisibleQuery.length === 0 ||
      [
        order.orderNumber,
        order.status,
        localizeOrderStatus(order.status, culture),
        order.id,
      ]
        .filter(Boolean)
        .some((value) =>
          value.toLowerCase().includes(normalizedVisibleQuery),
        );

    const matchesState =
      visibleState === "all" ||
      (visibleState === "attention" && isAttentionOrder(order)) ||
      (visibleState === "settled" && !isAttentionOrder(order));

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
      formatResource(copy.ordersStorefrontProductOfferDescription, {
        savingsPercent: input.savingsPercent,
        price: input.price,
      }),
    describeWithoutSavings: (product) =>
      product.shortDescription ?? copy.ordersStorefrontProductFallbackDescription,
    fallbackDescription: copy.ordersStorefrontProductFallbackDescription,
    formatMeta: (product) =>
      typeof product.compareAtPriceMinor === "number" &&
      product.compareAtPriceMinor > product.priceMinor
        ? formatResource(copy.ordersStorefrontProductOfferMeta, {
            compareAt: formatMoney(
              product.compareAtPriceMinor,
              product.currency,
              culture,
            ),
          })
        : null,
  });
  const cmsSpotlightCards = buildStorefrontPageSpotlightCards(cmsPages, {
    prefix: "orders",
    fallbackDescription: copy.ordersStorefrontCmsFallbackDescription,
  });
  const categorySpotlightCards = buildStorefrontCategorySpotlightLinkCards(categories, {
    prefix: "orders",
    fallbackDescription: copy.ordersStorefrontCatalogFallbackDescription,
  });
  const promotionLaneCards = buildMemberPromotionLaneCards(rankedProducts, culture);
  const sectionLinks = [
    { href: "#orders-overview", label: copy.ordersTitle },
    { href: "#orders-filters", label: copy.ordersFilterTitle },
    { href: "#orders-readiness", label: copy.ordersReadinessTitle },
    { href: "#orders-storefront", label: copy.ordersStorefrontWindowTitle },
    { href: "#orders-results", label: copy.openOrderCta },
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
            {copy.memberBreadcrumbOrders}
          </span>
        </nav>

        <div id="orders-overview" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.ordersEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.ordersTitle}
          </h1>
        </div>

        {status !== "ok" && (
          <StatusBanner
            tone="warning"
            title={copy.ordersWarningsTitle}
            message={formatResource(copy.ordersWarningsMessage, { status: statusLabel })}
          />
        )}

        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.ordersWindowTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.ordersWindowMessage, {
              count: filteredOrders.length,
              loadedCount: orders.length,
              currentPage,
              totalPages,
              status: statusLabel,
            })}
          </p>
        </div>

        <div id="orders-filters" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.ordersFilterTitle}
            </p>
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.ordersFilterMessage, {
                count: filteredOrders.length,
                loadedCount: orders.length,
              })}
            </p>
          </div>
          <form action={localizeHref("/orders", culture)} method="get" className="mt-5 grid gap-4 lg:grid-cols-[minmax(0,1fr)_220px_auto]">
            <label className="flex flex-col gap-2 text-sm text-[var(--color-text-secondary)]">
              <span>{copy.ordersFilterSearchPlaceholder}</span>
              <input
                type="search"
                name="visibleQuery"
                defaultValue={visibleQuery ?? ""}
                placeholder={copy.ordersFilterSearchPlaceholder}
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-base)] px-4 py-3 text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
              />
            </label>
            <label className="flex flex-col gap-2 text-sm text-[var(--color-text-secondary)]">
              <span>{copy.ordersFilterStateLabel}</span>
              <select
                name="visibleState"
                defaultValue={visibleState}
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-base)] px-4 py-3 text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
              >
                <option value="all">{copy.ordersFilterStateAll}</option>
                <option value="attention">{copy.ordersFilterStateAttention}</option>
                <option value="settled">{copy.ordersFilterStateSettled}</option>
              </select>
            </label>
            <div className="flex items-end gap-3">
              <button
                type="submit"
                className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-white transition hover:bg-[var(--color-brand-strong)]"
              >
                {copy.ordersFilterApplyCta}
              </button>
              {(visibleQuery || visibleState !== "all") && (
                <Link
                  href={localizeHref("/orders", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.clearFilterCta}
                </Link>
              )}
            </div>
          </form>
        </div>

        <div id="orders-readiness" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.ordersReadinessTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.ordersReadinessMessage, {
              count: attentionOrders.length,
              total: formatMoney(attentionGrossMinor, primaryCurrency, culture),
            })}
          </p>
          <div className="mt-5 grid gap-3 sm:grid-cols-3">
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                {copy.ordersReadinessVisibleLabel}
              </p>
              <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {orders.length}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                {copy.ordersReadinessAttentionLabel}
              </p>
              <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {attentionOrders.length}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                {copy.ordersReadinessValueLabel}
              </p>
              <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {formatMoney(attentionGrossMinor, primaryCurrency, culture)}
              </p>
            </div>
          </div>
          <div className="mt-5 flex flex-wrap gap-3">
            {attentionOrders[0] ? (
              <Link
            href={localizeHref(buildOrderPath(attentionOrders[0].id), culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.ordersReadinessPrimaryCta}
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
            title: copy.ordersTitle,
            description: formatResource(copy.ordersWindowMessage, {
              count: filteredOrders.length,
              loadedCount: orders.length,
              currentPage,
              totalPages,
              status: statusLabel,
            }),
            href: buildOrdersHref(currentPage, { visibleQuery, visibleState }),
            ctaLabel: copy.accountCompositionJourneyCurrentCta,
          }}
          nextCard={{
            label: copy.accountCompositionJourneyNextLabel,
            title: attentionOrders[0]?.orderNumber ?? copy.invoicesTitle,
            description: attentionOrders[0]
              ? formatResource(copy.ordersReadinessMessage, {
                  count: attentionOrders.length,
                  total: formatMoney(attentionGrossMinor, primaryCurrency, culture),
                })
              : copy.invoicesPortalNote,
    href: attentionOrders[0] ? buildOrderPath(attentionOrders[0].id) : "/invoices",
            ctaLabel: attentionOrders[0]
              ? copy.ordersReadinessPrimaryCta
              : copy.accountCompositionJourneyAddressesCta,
          }}
          routeMapItems={[
            {
              label: copy.accountCompositionRouteMapProfileLabel,
              title: copy.ordersRouteLabel,
              description: copy.ordersPortalNote,
              href: "/orders",
              ctaLabel: copy.accountCompositionRouteMapProfileCta,
            },
            {
              label: copy.accountCompositionRouteMapNextLabel,
              title: copy.invoicesTitle,
              description: copy.invoicesPortalNote,
              href: "/invoices",
              ctaLabel: copy.accountCompositionRouteMapAddressesCta,
            },
          ]}
          cmsPages={cmsPages}
          categories={categories}
          products={products}
        />

        <div id="orders-storefront" className="scroll-mt-28">
          <MemberStorefrontWindow
            culture={culture}
            title={copy.ordersStorefrontWindowTitle}
            message={formatResource(copy.ordersStorefrontWindowMessage, {
              cmsStatus: cmsPagesStatusLabel,
              categoriesStatus: categoriesStatusLabel,
              productsStatus: productsStatusLabel,
              pageCount: cmsPages.length,
              categoryCount: categories.length,
              productCount: products.length,
            })}
            cmsTitle={copy.ordersStorefrontCmsTitle}
            cmsCtaLabel={copy.ordersStorefrontCmsCta}
            cmsCards={cmsSpotlightCards}
            cmsEmptyMessage={formatResource(copy.ordersStorefrontCmsEmptyMessage, {
              status: cmsPagesStatusLabel,
            })}
            catalogTitle={copy.ordersStorefrontCatalogTitle}
            catalogCtaLabel={copy.ordersStorefrontCatalogCta}
            categoryCards={categorySpotlightCards}
            catalogEmptyMessage={formatResource(copy.ordersStorefrontCatalogEmptyMessage, {
              status: categoriesStatusLabel,
            })}
            productTitle={copy.ordersStorefrontProductTitle}
            productCtaLabel={copy.ordersStorefrontProductCta}
            productMessage={
              cartLinkedSlugSet.size > 0
                ? formatResource(copy.ordersStorefrontProductCartAwareMessage, {
                    count: cartLinkedSlugSet.size,
                  })
                : copy.ordersStorefrontProductMessage
            }
            productCards={storefrontOfferCards}
            productEmptyMessage={formatResource(copy.ordersStorefrontProductEmptyMessage, {
              status: productsStatusLabel,
            })}
            promotionLaneSectionTitle={copy.memberStorefrontPromotionLaneSectionTitle}
            promotionLaneSectionMessage={copy.memberStorefrontPromotionLaneSectionMessage}
            promotionLaneCards={promotionLaneCards}
            cartSectionTitle={copy.ordersStorefrontCartTitle}
            cartSectionMessage={
              storefrontCart && storefrontCart.items.length > 0
                ? formatResource(copy.ordersStorefrontCartMessage, {
                    status: storefrontCartStatusLabel,
                    count: storefrontCart.items.length,
                  })
                : formatResource(copy.ordersStorefrontCartEmptyMessage, {
                    status: storefrontCartStatusLabel,
                  })
            }
            cartSectionCartCtaLabel={copy.ordersStorefrontCartCta}
            cartSectionCheckoutCtaLabel={copy.ordersStorefrontCheckoutCta}
          />
        </div>

        <div id="orders-results" className="scroll-mt-28 grid gap-5">
          {filteredOrders.map((order) => (
            <article
              key={order.id}
              className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
            >
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {localizeOrderStatus(order.status, culture)}
                  </p>
                  <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                <Link href={localizeHref(buildOrderPath(order.id), culture)} className="transition hover:text-[var(--color-brand)]">
                      {order.orderNumber}
                    </Link>
                  </h2>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.createdLabel} {formatDateTime(order.createdAtUtc, culture)}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-lg font-semibold text-[var(--color-text-primary)]">
                    {formatMoney(order.grandTotalGrossMinor, order.currency, culture)}
                  </p>
                  <Link
                    href={localizeHref(buildOrderPath(order.id), culture)}
                    className="mt-3 inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.openOrderCta}
                  </Link>
                </div>
              </div>
            </article>
          ))}
        </div>

        {orders.length === 0 && (
          <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center">
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.noOrdersMessage}
            </p>
            <div className="mt-8 text-left">
              <MemberCrossSurfaceRail
                culture={culture}
                includeOrders={false}
                includeInvoices
                includeLoyalty
              />
            </div>
          </div>
        )}

        {orders.length > 0 && filteredOrders.length === 0 && (
          <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center">
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.ordersFilterEmptyMessage}
            </p>
          </div>
        )}

        {totalPages > 1 && (
          <div className="flex flex-wrap items-center gap-3">
            <Link
              aria-disabled={currentPage <= 1}
              href={localizeHref(
                buildOrdersHref(Math.max(1, currentPage - 1), {
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
                buildOrdersHref(Math.min(totalPages, currentPage + 1), {
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
          <MemberPortalNav culture={culture} activePath="/orders" />

          <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.ordersRouteLabel}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.ordersPortalNote}
            </p>
          </aside>

          <MemberCrossSurfaceRail
            culture={culture}
            includeOrders={false}
            includeInvoices
            includeLoyalty
          />
        </div>
      </div>
      </div>
    </section>
  );
}
