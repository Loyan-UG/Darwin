import Link from "next/link";
import { buildPromotionLaneRouteMapItem } from "@/components/composition-window-promotion-lane";
import { sortProductsByOpportunity } from "@/features/catalog/merchandising";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { CmsMetadataFocus } from "@/features/cms/discovery";
import type { PublicPageSummary } from "@/features/cms/types";
import { buildCatalogProductPath, buildCmsPagePath } from "@/lib/entity-paths";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { formatResource, getSharedResource } from "@/localization";

type CmsIndexCompositionWindowProps = {
  culture: string;
  searchQuery?: string;
  visibleState?: "all" | "ready" | "needs-attention";
  visibleSort?: "featured" | "title-asc" | "ready-first" | "attention-first";
  metadataFocus?: CmsMetadataFocus;
  currentPage: number;
  totalItems: number;
  pages: PublicPageSummary[];
  categories: PublicCategorySummary[];
  products: PublicProductSummary[];
  cartSummary: {
    status: string;
    itemCount: number;
    currency: string;
    grandTotalGrossMinor: number;
  } | null;
  reviewHref: string;
  reviewLabel: string;
};

export function CmsIndexCompositionWindow({
  culture,
  searchQuery,
  visibleState = "all",
  visibleSort = "featured",
  metadataFocus = "all",
  currentPage,
  totalItems,
  pages,
  categories,
  products,
  cartSummary,
  reviewHref,
  reviewLabel,
}: CmsIndexCompositionWindowProps) {
  const copy = getSharedResource(culture);
  const spotlightPage = pages[0] ?? null;
  const strongestCategory = categories[0] ?? null;
  const strongestProduct = sortProductsByOpportunity(products)[0] ?? null;
  const currentWindowHref = localizeHref(
    buildAppQueryPath("/cms", {
      page: currentPage > 1 ? currentPage : undefined,
      search: searchQuery,
      visibleState: visibleState !== "all" ? visibleState : undefined,
      visibleSort: visibleSort !== "featured" ? visibleSort : undefined,
      metadataFocus: metadataFocus !== "all" ? metadataFocus : undefined,
    }),
    culture,
  );
  const reviewWindowHref = localizeHref(reviewHref, culture);
  const contentHref = localizeHref(
    spotlightPage ? buildCmsPagePath(spotlightPage.slug) : "/cms",
    culture,
  );
  const storefrontHref = localizeHref(
    cartSummary && cartSummary.itemCount > 0
      ? "/cart"
      : strongestProduct
        ? buildCatalogProductPath(strongestProduct.slug)
        : strongestCategory
          ? buildAppQueryPath("/catalog", { category: strongestCategory.slug })
          : "/catalog",
    culture,
  );

  const promotionLaneRouteMapItem = buildPromotionLaneRouteMapItem({
    id: "cms-index-composition-route-promotion-lane",
    products,
    culture,
    copy: {
      cardLabel: copy.cmsCampaignPromotionLaneCardLabel,
      heroLabel: copy.cmsCampaignPromotionLaneHeroLabel,
      valueLabel: copy.cmsCampaignPromotionLaneValueLabel,
      liveOffersLabel: copy.cmsCampaignPromotionLaneLiveOffersLabel,
      baseLabel: copy.cmsCampaignPromotionLaneBaseLabel,
      title: copy.cmsCampaignPromotionLaneTitle,
      fallbackTitle: copy.cmsCampaignPromotionLaneFallbackTitle,
      description: copy.cmsCampaignPromotionLaneDescription,
      fallbackDescription: copy.cmsCampaignPromotionLaneFallbackDescription,
      cta: copy.cmsCampaignPromotionLaneCta,
      meta: copy.cmsCampaignPromotionLaneMeta,
    },
  });
  const routeMapItems = [
    {
      id: "cms-index-composition-current",
      label: copy.cmsIndexCompositionRouteMapCurrentLabel,
      title: copy.cmsIndexCompositionRouteMapCurrentTitle,
      description: formatResource(
        copy.cmsIndexCompositionRouteMapCurrentDescription,
        {
          count: totalItems,
          page: currentPage,
        },
      ),
      href: currentWindowHref,
      ctaLabel: copy.cmsIndexCompositionRouteMapCurrentCta,
      meta: formatResource(copy.cmsIndexCompositionRouteMapCurrentMeta, {
        count: totalItems,
      }),
    },
    {
      id: "cms-index-composition-review",
      label: copy.cmsIndexCompositionRouteMapReviewLabel,
      title: copy.cmsIndexCompositionRouteMapReviewTitle,
      description: copy.cmsIndexCompositionRouteMapReviewDescription,
      href: reviewWindowHref,
      ctaLabel: reviewLabel,
      meta: formatResource(copy.cmsIndexCompositionRouteMapReviewMeta, {
        count: pages.length,
      }),
    },
    {
      id: "cms-index-composition-content",
      label: copy.cmsIndexCompositionRouteMapContentLabel,
      title: spotlightPage
        ? formatResource(copy.cmsIndexCompositionRouteMapContentTitle, {
            title: spotlightPage.title,
          })
        : copy.cmsIndexCompositionRouteMapContentFallbackTitle,
      description:
        spotlightPage?.metaDescription ??
        copy.cmsIndexCompositionRouteMapContentFallbackDescription,
      href: contentHref,
      ctaLabel: copy.cmsIndexCompositionRouteMapContentCta,
      meta: formatResource(copy.cmsIndexCompositionRouteMapContentMeta, {
        count: pages.length,
      }),
    },
    {
      id: "cms-index-composition-storefront",
      label: copy.cmsIndexCompositionRouteMapStorefrontLabel,
      title: cartSummary && cartSummary.itemCount > 0
        ? copy.cmsIndexCompositionRouteMapStorefrontCartTitle
        : strongestProduct
          ? formatResource(copy.cmsIndexCompositionRouteMapStorefrontProductTitle, {
              product: strongestProduct.name,
            })
          : strongestCategory
            ? formatResource(
                copy.cmsIndexCompositionRouteMapStorefrontCategoryTitle,
                {
                  category: strongestCategory.name,
                },
              )
            : copy.cmsIndexCompositionRouteMapStorefrontFallbackTitle,
      description: cartSummary && cartSummary.itemCount > 0
        ? formatResource(copy.cmsIndexCompositionRouteMapStorefrontCartDescription, {
            itemCount: cartSummary.itemCount,
            total: formatMoney(
              cartSummary.grandTotalGrossMinor,
              cartSummary.currency,
              culture,
            ),
          })
        : strongestProduct
          ? formatResource(
              copy.cmsIndexCompositionRouteMapStorefrontProductDescription,
              {
                price: formatMoney(
                  strongestProduct.priceMinor,
                  strongestProduct.currency,
                  culture,
                ),
              },
            )
          : strongestCategory?.description ??
            copy.cmsIndexCompositionRouteMapStorefrontFallbackDescription,
      href: storefrontHref,
      ctaLabel: cartSummary && cartSummary.itemCount > 0
        ? copy.cmsIndexCompositionRouteMapStorefrontCartCta
        : copy.cmsIndexCompositionRouteMapStorefrontCta,
      meta: cartSummary && cartSummary.itemCount > 0
        ? formatResource(copy.cmsIndexCompositionRouteMapStorefrontCartMeta, {
            count: cartSummary.itemCount,
          })
        : strongestProduct
          ? formatResource(copy.cmsIndexCompositionRouteMapStorefrontProductMeta, {
              count: products.length,
            })
          : formatResource(copy.cmsIndexCompositionRouteMapStorefrontCategoryMeta, {
              count: categories.length,
            }),
    },
    promotionLaneRouteMapItem,
  ];

  return (
    <div className="grid gap-5 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)]">
      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
          {copy.cmsIndexCompositionJourneyTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.cmsIndexCompositionJourneyMessage}
        </p>
        <div className="mt-5 grid gap-3">
          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.cmsIndexCompositionJourneyCurrentLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {copy.cmsIndexCompositionJourneyCurrentTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.cmsIndexCompositionJourneyCurrentDescription, {
                count: totalItems,
                page: currentPage,
              })}
            </p>
            <div className="mt-4">
              <Link
                href={currentWindowHref}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.cmsIndexCompositionJourneyCurrentCta}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.cmsIndexCompositionJourneyReviewLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {copy.cmsIndexCompositionJourneyReviewTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.cmsIndexCompositionJourneyReviewDescription}
            </p>
            <div className="mt-4">
              <Link
                href={reviewWindowHref}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {reviewLabel}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.cmsIndexCompositionJourneyStorefrontLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {cartSummary && cartSummary.itemCount > 0
                ? copy.cmsIndexCompositionJourneyStorefrontCartTitle
                : strongestProduct
                  ? formatResource(
                      copy.cmsIndexCompositionJourneyStorefrontProductTitle,
                      {
                        product: strongestProduct.name,
                      },
                    )
                  : strongestCategory
                    ? formatResource(
                        copy.cmsIndexCompositionJourneyStorefrontCategoryTitle,
                        {
                          category: strongestCategory.name,
                        },
                      )
                    : copy.cmsIndexCompositionJourneyStorefrontFallbackTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {cartSummary && cartSummary.itemCount > 0
                ? formatResource(
                    copy.cmsIndexCompositionJourneyStorefrontCartDescription,
                    {
                      itemCount: cartSummary.itemCount,
                      total: formatMoney(
                        cartSummary.grandTotalGrossMinor,
                        cartSummary.currency,
                        culture,
                      ),
                    },
                  )
                : strongestProduct
                  ? formatResource(
                      copy.cmsIndexCompositionJourneyStorefrontProductDescription,
                      {
                        price: formatMoney(
                          strongestProduct.priceMinor,
                          strongestProduct.currency,
                          culture,
                        ),
                      },
                    )
                  : strongestCategory?.description ??
                    copy.cmsIndexCompositionJourneyStorefrontFallbackDescription}
            </p>
            <div className="mt-4">
              <Link
                href={storefrontHref}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.cmsIndexCompositionJourneyStorefrontCta}
              </Link>
            </div>
          </article>
        </div>
      </section>

      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
          {copy.cmsIndexCompositionRouteMapTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.cmsIndexCompositionRouteMapMessage}
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



