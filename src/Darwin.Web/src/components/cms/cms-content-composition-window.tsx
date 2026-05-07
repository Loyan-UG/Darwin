import Link from "next/link";
import { buildPromotionLaneRouteMapItem } from "@/components/composition-window-promotion-lane";
import { sortProductsByOpportunity } from "@/features/catalog/merchandising";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import { buildCmsReviewTargetHref } from "@/features/review/review-window";
import type { PublicPageDetail, PublicPageSummary } from "@/features/cms/types";
import { buildCatalogProductPath } from "@/lib/entity-paths";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { formatResource, getSharedResource } from "@/localization";

type CmsContentCompositionWindowProps = {
  culture: string;
  page: PublicPageDetail;
  pagePath: string;
  headings: Array<{ id: string; text: string }>;
  readingMinutes: number;
  relatedPages: PublicPageSummary[];
  categories: PublicCategorySummary[];
  products: PublicProductSummary[];
  cartSummary: {
    status: string;
    itemCount: number;
    currency: string;
    grandTotalGrossMinor: number;
  } | null;
  reviewWindow?: {
    visibleQuery?: string;
    visibleState?: "all" | "ready" | "needs-attention";
    visibleSort?: "featured" | "title-asc" | "ready-first" | "attention-first";
    metadataFocus?: "all" | "missing-title" | "missing-description" | "missing-both";
  };
  reviewPrimaryHref: string;
  reviewPrimaryLabel: string;
  reviewNextPage: PublicPageSummary | null;
};

export function CmsContentCompositionWindow({
  culture,
  page,
  pagePath,
  headings,
  readingMinutes,
  relatedPages,
  categories,
  products,
  cartSummary,
  reviewWindow,
  reviewPrimaryHref,
  reviewPrimaryLabel,
  reviewNextPage,
}: CmsContentCompositionWindowProps) {
  const copy = getSharedResource(culture);
  const readingHref =
    headings[0]?.id ? `${pagePath}#${headings[0].id}` : pagePath;
  const strongestCategory = categories[0] ?? null;
  const strongestProduct = sortProductsByOpportunity(products)[0] ?? null;
  const publishedSetHref = buildAppQueryPath("/cms", {
    visibleQuery: reviewWindow?.visibleQuery,
    visibleState:
      reviewWindow?.visibleState && reviewWindow.visibleState !== "all"
        ? reviewWindow.visibleState
        : undefined,
    visibleSort:
      reviewWindow?.visibleSort && reviewWindow.visibleSort !== "featured"
        ? reviewWindow.visibleSort
        : undefined,
    metadataFocus:
      reviewWindow?.metadataFocus && reviewWindow.metadataFocus !== "all"
        ? reviewWindow.metadataFocus
        : undefined,
  });
  const commerceHref = cartSummary && cartSummary.itemCount > 0
    ? "/checkout"
    : strongestProduct
      ? buildCatalogProductPath(strongestProduct.slug)
      : strongestCategory
        ? buildAppQueryPath("/catalog", { category: strongestCategory.slug })
        : "/catalog";
  const commerceDescription = cartSummary && cartSummary.itemCount > 0
    ? formatResource(copy.cmsCompositionJourneyCommerceCartDescription, {
        itemCount: cartSummary.itemCount,
        total: formatMoney(
          cartSummary.grandTotalGrossMinor,
          cartSummary.currency,
          culture,
        ),
      })
    : strongestProduct
      ? formatResource(copy.cmsCompositionJourneyCommerceProductDescription, {
          product: strongestProduct.name,
          price: formatMoney(
            strongestProduct.priceMinor,
            strongestProduct.currency,
            culture,
          ),
        })
      : strongestCategory
        ? formatResource(copy.cmsCompositionJourneyCommerceCategoryDescription, {
            category: strongestCategory.name,
          })
        : copy.cmsCompositionJourneyCommerceFallbackDescription;
  const commerceLabel = cartSummary && cartSummary.itemCount > 0
    ? copy.cmsCompositionJourneyCommerceCartLabel
    : strongestProduct
      ? copy.cmsCompositionJourneyCommerceProductLabel
      : copy.cmsCompositionJourneyCommerceCategoryLabel;
  const promotionLaneRouteMapItem = buildPromotionLaneRouteMapItem({
    id: "cms-composition-route-promotion-lane",
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
      id: "cms-composition-route-published",
      label: copy.cmsCompositionRouteMapPublishedLabel,
      title: copy.cmsCompositionRouteMapPublishedTitle,
      description: formatResource(copy.cmsCompositionRouteMapPublishedDescription, {
        count: relatedPages.length,
      }),
      href: publishedSetHref,
      ctaLabel: copy.cmsCompositionRouteMapPublishedCta,
      meta: page.title,
    },
    {
      id: "cms-composition-route-category",
      label: copy.cmsCompositionRouteMapCategoryLabel,
      title: strongestCategory
        ? formatResource(copy.cmsCompositionRouteMapCategoryTitle, {
            category: strongestCategory.name,
          })
        : copy.cmsCompositionRouteMapCategoryFallbackTitle,
      description: strongestCategory
        ? strongestCategory.description ??
          copy.cmsCompositionRouteMapCategoryFallbackDescription
        : copy.cmsCompositionRouteMapCategoryFallbackDescription,
      href: strongestCategory
        ? buildAppQueryPath("/catalog", { category: strongestCategory.slug })
        : "/catalog",
      ctaLabel: copy.cmsCompositionRouteMapCategoryCta,
      meta: formatResource(copy.cmsCompositionRouteMapCategoryMeta, {
        count: categories.length,
      }),
    },
    {
      id: "cms-composition-route-product",
      label: copy.cmsCompositionRouteMapProductLabel,
      title: strongestProduct
        ? formatResource(copy.cmsCompositionRouteMapProductTitle, {
            product: strongestProduct.name,
          })
        : copy.cmsCompositionRouteMapProductFallbackTitle,
      description: strongestProduct
        ? formatResource(copy.cmsCompositionRouteMapProductDescription, {
            price: formatMoney(
              strongestProduct.priceMinor,
              strongestProduct.currency,
              culture,
            ),
          })
        : copy.cmsCompositionRouteMapProductFallbackDescription,
      href: strongestProduct ? buildCatalogProductPath(strongestProduct.slug) : "/catalog",
      ctaLabel: copy.cmsCompositionRouteMapProductCta,
      meta: formatResource(copy.cmsCompositionRouteMapProductMeta, {
        count: products.length,
      }),
    },
    promotionLaneRouteMapItem,
  ];

  return (
    <>
      <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
          {copy.cmsCompositionJourneyTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.cmsCompositionJourneyMessage}
        </p>
        <div className="mt-5 grid gap-3">
          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.cmsCompositionJourneyReadLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {copy.cmsCompositionJourneyReadTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.cmsCompositionJourneyReadDescription, {
                minutes: readingMinutes,
                sectionCount: headings.length,
              })}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref(readingHref, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.cmsCompositionJourneyReadCta}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.cmsCompositionJourneyReviewLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {reviewNextPage
                ? formatResource(copy.cmsCompositionJourneyReviewTitle, {
                    title: reviewNextPage.title,
                  })
                : copy.cmsCompositionJourneyReviewFallbackTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {reviewNextPage
                ? copy.cmsCompositionJourneyReviewDescription
                : copy.cmsCompositionJourneyReviewFallbackDescription}
            </p>
            <div className="mt-4 flex flex-wrap gap-3">
              <Link
                href={localizeHref(reviewPrimaryHref, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {reviewPrimaryLabel}
              </Link>
              {reviewNextPage ? (
                <Link
                  href={localizeHref(
                    buildCmsReviewTargetHref(reviewNextPage.slug, reviewWindow),
                    culture,
                  )}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {copy.cmsCompositionJourneyReviewSecondaryCta}
                </Link>
              ) : null}
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {commerceLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {copy.cmsCompositionJourneyCommerceTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {commerceDescription}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref(commerceHref, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.cmsCompositionJourneyCommerceCta}
              </Link>
            </div>
          </article>
        </div>
      </div>

      <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
          {copy.cmsCompositionRouteMapTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.cmsCompositionRouteMapMessage}
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
              {item.meta ? (
                <p className="mt-3 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {item.meta}
                </p>
              ) : null}
              <div className="mt-4">
                <Link
                  href={localizeHref(item.href, culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {item.ctaLabel}
                </Link>
              </div>
            </article>
          ))}
        </div>
      </div>
    </>
  );
}



