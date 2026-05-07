import Link from "next/link";
import { buildPromotionLaneRouteMapItem } from "@/components/composition-window-promotion-lane";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import { buildCatalogProductPath, buildCmsPagePath } from "@/lib/entity-paths";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { formatResource, getCatalogResource } from "@/localization";

type CatalogContentCompositionWindowProps = {
  culture: string;
  activeCategory: PublicCategorySummary | null;
  cmsPages: PublicPageSummary[];
  products: PublicProductSummary[];
  cartSummary: {
    status: string;
    itemCount: number;
    currency: string;
    grandTotalGrossMinor: number;
  } | null;
  totalProducts: number;
  currentPage: number;
  searchQuery?: string;
  reviewHref: string;
  reviewLabel: string;
};

export function CatalogContentCompositionWindow({
  culture,
  activeCategory,
  cmsPages,
  products,
  cartSummary,
  totalProducts,
  currentPage,
  searchQuery,
  reviewHref,
  reviewLabel,
}: CatalogContentCompositionWindowProps) {
  const copy = getCatalogResource(culture);
  const spotlightPage = cmsPages[0] ?? null;
  const spotlightProduct = products[0] ?? null;
  const currentCatalogHref = localizeHref(
    buildAppQueryPath("/catalog", {
      category: activeCategory?.slug,
      search: searchQuery,
    }),
    culture,
  );
  const contentHref = localizeHref(
    spotlightPage ? buildCmsPagePath(spotlightPage.slug) : "/cms",
    culture,
  );
  const storefrontHref = localizeHref(
    spotlightProduct
      ? buildCatalogProductPath(spotlightProduct.slug)
      : activeCategory
        ? buildAppQueryPath("/catalog", { category: activeCategory.slug })
        : "/catalog",
    culture,
  );
  const cartHref = localizeHref(cartSummary ? "/cart" : "/account", culture);

  const promotionLaneRouteMapItem = buildPromotionLaneRouteMapItem({
    id: "catalog-composition-route-promotion-lane",
    products,
    culture,
    copy: {
      cardLabel: copy.campaignWindowPromotionLaneLabel,
      heroLabel: copy.campaignWindowPromotionLaneHeroLabel,
      valueLabel: copy.campaignWindowPromotionLaneValueLabel,
      liveOffersLabel: copy.campaignWindowPromotionLaneLiveOffersLabel,
      baseLabel: copy.campaignWindowPromotionLaneBaseLabel,
      title: copy.campaignWindowPromotionLaneTitle,
      fallbackTitle: copy.campaignWindowPromotionLaneFallbackTitle,
      description: copy.campaignWindowPromotionLaneDescription,
      fallbackDescription: copy.campaignWindowPromotionLaneFallbackDescription,
      cta: copy.campaignWindowPromotionLaneCta,
      meta: copy.campaignWindowPromotionLaneMeta,
    },
  });
  const routeMapItems = [
    {
      id: "catalog-route-current",
      label: copy.catalogCompositionRouteMapCurrentLabel,
      title: activeCategory
        ? formatResource(copy.catalogCompositionRouteMapCurrentCategoryTitle, {
            category: activeCategory.name,
          })
        : copy.catalogCompositionRouteMapCurrentTitle,
      description: formatResource(copy.catalogCompositionRouteMapCurrentDescription, {
        count: totalProducts,
        page: currentPage,
      }),
      href: currentCatalogHref,
      ctaLabel: copy.catalogCompositionRouteMapCurrentCta,
      meta: formatResource(copy.catalogCompositionRouteMapCurrentMeta, {
        count: totalProducts,
      }),
    },
    {
      id: "catalog-route-review",
      label: copy.catalogCompositionRouteMapReviewLabel,
      title: copy.catalogCompositionRouteMapReviewTitle,
      description: copy.catalogCompositionRouteMapReviewDescription,
      href: localizeHref(reviewHref, culture),
      ctaLabel: reviewLabel,
      meta: formatResource(copy.catalogCompositionRouteMapReviewMeta, {
        count: products.length,
      }),
    },
    {
      id: "catalog-route-content",
      label: copy.catalogCompositionRouteMapContentLabel,
      title: spotlightPage
        ? formatResource(copy.catalogCompositionRouteMapContentTitle, {
            title: spotlightPage.title,
          })
        : copy.catalogCompositionRouteMapContentFallbackTitle,
      description:
        spotlightPage?.metaDescription ??
        copy.catalogCompositionRouteMapContentFallbackDescription,
      href: contentHref,
      ctaLabel: copy.catalogCompositionRouteMapContentCta,
      meta: formatResource(copy.catalogCompositionRouteMapContentMeta, {
        count: cmsPages.length,
      }),
    },
    {
      id: "catalog-route-account",
      label: copy.catalogCompositionRouteMapAccountLabel,
      title: cartSummary
        ? copy.catalogCompositionRouteMapAccountCartTitle
        : copy.catalogCompositionRouteMapAccountTitle,
      description: cartSummary
        ? formatResource(copy.catalogCompositionRouteMapAccountCartDescription, {
            itemCount: cartSummary.itemCount,
            total: formatMoney(
              cartSummary.grandTotalGrossMinor,
              cartSummary.currency,
              culture,
            ),
          })
        : copy.catalogCompositionRouteMapAccountDescription,
      href: cartHref,
      ctaLabel: cartSummary
        ? copy.catalogCompositionRouteMapAccountCartCta
        : copy.catalogCompositionRouteMapAccountCta,
      meta: cartSummary
        ? formatResource(copy.catalogCompositionRouteMapAccountCartMeta, {
            count: cartSummary.itemCount,
          })
        : copy.catalogCompositionRouteMapAccountMeta,
    },
    promotionLaneRouteMapItem,
  ];

  return (
    <div className="grid gap-5 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)]">
      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
          {copy.catalogCompositionJourneyTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.catalogCompositionJourneyMessage}
        </p>
        <div className="mt-5 grid gap-3">
          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.catalogCompositionJourneyCurrentLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {activeCategory
                ? formatResource(copy.catalogCompositionJourneyCurrentCategoryTitle, {
                    category: activeCategory.name,
                  })
                : copy.catalogCompositionJourneyCurrentTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.catalogCompositionJourneyCurrentDescription, {
                count: totalProducts,
                page: currentPage,
              })}
            </p>
            <div className="mt-4">
              <Link
                href={currentCatalogHref}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.catalogCompositionJourneyCurrentCta}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.catalogCompositionJourneyNextLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {copy.catalogCompositionJourneyNextTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.catalogCompositionJourneyNextDescription}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref(reviewHref, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {reviewLabel}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.catalogCompositionJourneyStorefrontLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {spotlightProduct
                ? formatResource(copy.catalogCompositionJourneyStorefrontTitle, {
                    product: spotlightProduct.name,
                  })
                : activeCategory
                  ? formatResource(copy.catalogCompositionJourneyStorefrontCategoryTitle, {
                      category: activeCategory.name,
                    })
                  : copy.catalogCompositionJourneyStorefrontFallbackTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {spotlightProduct
                ? formatResource(copy.catalogCompositionJourneyStorefrontDescription, {
                    price: formatMoney(
                      spotlightProduct.priceMinor,
                      spotlightProduct.currency,
                      culture,
                    ),
                  })
                : activeCategory?.description ??
                  copy.catalogCompositionJourneyStorefrontFallbackDescription}
            </p>
            <div className="mt-4">
              <Link
                href={storefrontHref}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.catalogCompositionJourneyStorefrontCta}
              </Link>
            </div>
          </article>
        </div>
      </section>

      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
          {copy.catalogCompositionRouteMapTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.catalogCompositionRouteMapMessage}
        </p>
        <div className="mt-5 grid gap-3">
          {routeMapItems.map((item) => (
            <article
              key={item.id}
              className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
            >
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {item.label}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {item.title}
              </p>
              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                {item.description}
              </p>
              <p className="mt-3 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {item.meta}
              </p>
              <div className="mt-4">
                <Link
                  href={item.href}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {item.ctaLabel}
                </Link>
              </div>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}



