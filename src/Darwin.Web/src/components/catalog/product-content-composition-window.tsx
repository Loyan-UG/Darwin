import Link from "next/link";
import { buildPromotionLaneRouteMapItem } from "@/components/composition-window-promotion-lane";
import type {
  PublicCategorySummary,
  PublicProductDetail,
  PublicProductSummary,
} from "@/features/catalog/types";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { formatResource, getCatalogResource } from "@/localization";

type ProductContentCompositionWindowProps = {
  culture: string;
  product: PublicProductDetail;
  primaryCategory: PublicCategorySummary | null;
  reviewProducts: PublicProductSummary[];
  relatedProducts: PublicProductSummary[];
  cartSummary: {
    status: string;
    itemCount: number;
    currency: string;
    grandTotalGrossMinor: number;
  } | null;
  reviewCatalogPath: string;
  reviewPrimaryLabel: string;
  nextReviewProduct: PublicProductSummary | null;
};

export function ProductContentCompositionWindow({
  culture,
  product,
  primaryCategory,
  reviewProducts,
  relatedProducts,
  cartSummary,
  reviewCatalogPath,
  reviewPrimaryLabel,
  nextReviewProduct,
}: ProductContentCompositionWindowProps) {
  const copy = getCatalogResource(culture);
  const hasOffer =
    typeof product.compareAtPriceMinor === "number" &&
    product.compareAtPriceMinor > product.priceMinor;
  const mediaCount = product.media.length;
  const cartHref = cartSummary && cartSummary.itemCount > 0 ? "/checkout" : "/cart";
  const browseHref = primaryCategory
    ? buildAppQueryPath("/catalog", { category: primaryCategory.slug })
    : "/catalog";
  const promotionLaneRouteMapItem = buildPromotionLaneRouteMapItem({
    id: "product-composition-route-promotion-lane",
    products: reviewProducts.length > 0 ? reviewProducts : relatedProducts,
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

  return (
    <div className="grid gap-5 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)]">
      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
          {copy.productCompositionJourneyTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.productCompositionJourneyMessage}
        </p>
        <div className="mt-5 grid gap-3">
          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.productCompositionJourneyBrowseLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {primaryCategory
                ? formatResource(copy.productCompositionJourneyBrowseTitle, {
                    category: primaryCategory.name,
                  })
                : copy.productCompositionJourneyBrowseFallbackTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {primaryCategory
                ? formatResource(copy.productCompositionJourneyBrowseDescription, {
                    relatedCount: relatedProducts.length,
                  })
                : copy.productCompositionJourneyBrowseFallbackDescription}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref(browseHref, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.productCompositionJourneyBrowseCta}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.productCompositionJourneyReviewLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {nextReviewProduct
                ? formatResource(copy.productCompositionJourneyReviewTitle, {
                    product: nextReviewProduct.name,
                  })
                : copy.productCompositionJourneyReviewFallbackTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {nextReviewProduct
                ? formatResource(copy.productCompositionJourneyReviewDescription, {
                    reviewCount: reviewProducts.length,
                  })
                : copy.productCompositionJourneyReviewFallbackDescription}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref(reviewCatalogPath, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {reviewPrimaryLabel}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {cartSummary && cartSummary.itemCount > 0
                ? copy.productCompositionJourneyCheckoutLabel
                : copy.productCompositionJourneyCartLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {copy.productCompositionJourneyCommerceTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {cartSummary && cartSummary.itemCount > 0
                ? formatResource(copy.productCompositionJourneyCheckoutDescription, {
                    itemCount: cartSummary.itemCount,
                    total: formatMoney(
                      cartSummary.grandTotalGrossMinor,
                      cartSummary.currency,
                      culture,
                    ),
                  })
                : hasOffer
                  ? formatResource(copy.productCompositionJourneyCartOfferDescription, {
                      price: formatMoney(product.priceMinor, product.currency, culture),
                    })
                  : formatResource(copy.productCompositionJourneyCartBaseDescription, {
                      price: formatMoney(product.priceMinor, product.currency, culture),
                    })}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref(cartHref, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.productCompositionJourneyCommerceCta}
              </Link>
            </div>
          </article>

        </div>
      </section>

      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
          {copy.productCompositionRouteMapTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.productCompositionRouteMapMessage}
        </p>
        <div className="mt-5 grid gap-3">
          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.productCompositionRouteMapCatalogLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {primaryCategory
                ? formatResource(copy.productCompositionRouteMapCatalogTitle, {
                    category: primaryCategory.name,
                  })
                : copy.productCompositionRouteMapCatalogFallbackTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {primaryCategory
                ? formatResource(copy.productCompositionRouteMapCatalogDescription, {
                    relatedCount: relatedProducts.length,
                  })
                : copy.productCompositionRouteMapCatalogFallbackDescription}
            </p>
            <p className="mt-3 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {formatResource(copy.productCompositionRouteMapCatalogMeta, {
                reviewCount: reviewProducts.length,
              })}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref(browseHref, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.productCompositionRouteMapCatalogCta}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.productCompositionRouteMapMediaLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {formatResource(copy.productCompositionRouteMapMediaTitle, {
                count: mediaCount,
              })}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.productCompositionRouteMapMediaDescription, {
                variantCount: product.variants.length,
              })}
            </p>
            <p className="mt-3 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {product.slug}
            </p>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.productCompositionRouteMapOfferLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {hasOffer
                ? copy.productCompositionRouteMapOfferTitle
                : copy.productCompositionRouteMapOfferFallbackTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {hasOffer
                ? formatResource(copy.productCompositionRouteMapOfferDescription, {
                    price: formatMoney(product.priceMinor, product.currency, culture),
                  })
                : formatResource(copy.productCompositionRouteMapOfferFallbackDescription, {
                    price: formatMoney(product.priceMinor, product.currency, culture),
                  })}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref(cartHref, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.productCompositionRouteMapOfferCta}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {promotionLaneRouteMapItem.label}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {promotionLaneRouteMapItem.title}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {promotionLaneRouteMapItem.description}
            </p>
            <p className="mt-3 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {promotionLaneRouteMapItem.meta}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref(promotionLaneRouteMapItem.href, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {promotionLaneRouteMapItem.ctaLabel}
              </Link>
            </div>
          </article>
        </div>
      </section>
    </div>
  );
}




