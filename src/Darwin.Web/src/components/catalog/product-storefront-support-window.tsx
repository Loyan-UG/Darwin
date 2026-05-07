import Link from "next/link";
import { StorefrontCampaignBoard } from "@/components/storefront/storefront-campaign-board";
import { StorefrontOfferBoard } from "@/components/storefront/storefront-offer-board";
import { StorefrontSpotlightBoard } from "@/components/storefront/storefront-spotlight-board";
import { sortProductsByOpportunity } from "@/features/catalog/merchandising";
import { summarizeCatalogPromotionLanes } from "@/features/catalog/promotion-lanes";
import type { PublicCategorySummary, PublicProductSummary } from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  buildStorefrontCategorySpotlightLinkCards,
  buildStorefrontOfferCards,
  buildStorefrontPageSpotlightCards,
} from "@/features/storefront/storefront-campaigns";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import {
  formatResource,
  getCatalogResource,
  resolveApiStatusLabel,
} from "@/localization";

type ProductStorefrontSupportWindowProps = {
  culture: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
  cartSummary: {
    status: string;
    itemCount: number;
    currency: string;
    grandTotalGrossMinor: number;
  } | null;
};

export function ProductStorefrontSupportWindow({
  culture,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  cartSummary,
}: ProductStorefrontSupportWindowProps) {
  const copy = getCatalogResource(culture);
  const cmsPagesStatusLabel =
    resolveApiStatusLabel(cmsPagesStatus, copy) ?? cmsPagesStatus;
  const categoriesStatusLabel =
    resolveApiStatusLabel(categoriesStatus, copy) ?? categoriesStatus;
  const productsStatusLabel =
    resolveApiStatusLabel(productsStatus, copy) ?? productsStatus;
  const rankedProducts = sortProductsByOpportunity(products);
  const cmsCards = buildStorefrontPageSpotlightCards(cmsPages, {
    prefix: "product-support-cms",
    fallbackDescription: copy.productCmsWindowFallbackDescription,
  });
  const categoryCards = buildStorefrontCategorySpotlightLinkCards(categories, {
    prefix: "product-support-category",
    fallbackDescription: copy.productCatalogWindowFallbackDescription,
  });
  const promotionLaneCards = summarizeCatalogPromotionLanes(rankedProducts).map((entry) => {
    const laneLabel =
      entry.lane === "hero-offers"
        ? copy.campaignWindowPromotionLaneHeroLabel
        : entry.lane === "value-offers"
          ? copy.campaignWindowPromotionLaneValueLabel
          : entry.lane === "live-offers"
            ? copy.campaignWindowPromotionLaneLiveOffersLabel
            : copy.campaignWindowPromotionLaneBaseLabel;
    const href =
      entry.lane === "hero-offers"
        ? buildAppQueryPath("/catalog", { visibleState: "offers", visibleSort: "offers-first", savingsBand: "hero" })
        : entry.lane === "value-offers"
          ? buildAppQueryPath("/catalog", { visibleState: "offers", visibleSort: "offers-first", savingsBand: "value" })
          : entry.lane === "live-offers"
            ? buildAppQueryPath("/catalog", { visibleState: "offers", visibleSort: "savings-desc" })
            : buildAppQueryPath("/catalog", { visibleState: "base", visibleSort: "base-first" });

    return {
      id: `product-support-promotion-${entry.lane}`,
      label: copy.campaignWindowPromotionLaneLabel,
      title: entry.anchorProduct
        ? formatResource(copy.campaignWindowPromotionLaneTitle, {
            lane: laneLabel,
            product: entry.anchorProduct.name,
          })
        : formatResource(copy.campaignWindowPromotionLaneFallbackTitle, {
            lane: laneLabel,
          }),
      description: entry.anchorProduct
        ? formatResource(copy.campaignWindowPromotionLaneDescription, {
            lane: laneLabel,
            count: entry.count,
            price: formatMoney(entry.anchorProduct.priceMinor, entry.anchorProduct.currency, culture),
          })
        : formatResource(copy.campaignWindowPromotionLaneFallbackDescription, {
            lane: laneLabel,
          }),
      href,
      ctaLabel: copy.campaignWindowPromotionLaneCta,
      meta: formatResource(copy.campaignWindowPromotionLaneMeta, {
        count: entry.count,
      }),
    };
  });
  const productCards = buildStorefrontOfferCards(rankedProducts, {
    labels: {
      heroOffer: copy.campaignWindowProductHeroLabel,
      valueOffer: copy.campaignWindowProductValueLabel,
      priceDrop: copy.campaignWindowProductPriceDropLabel,
      steadyPick: copy.campaignWindowProductSteadyLabel,
    },
    formatPrice: (product) => formatMoney(product.priceMinor, product.currency, culture),
    describeWithSavings: (_, input) =>
      formatResource(copy.productProductsWindowOfferDescription, {
        savingsPercent: input.savingsPercent,
        price: input.price,
      }),
    describeWithoutSavings: (product) =>
      product.shortDescription ?? copy.productProductsWindowFallbackDescription,
    fallbackDescription: copy.productProductsWindowFallbackDescription,
    ctaLabel: copy.productProductsWindowCta,
  });

  return (
    <div
      id="product-storefront-support"
      className="scroll-mt-28 grid gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]"
    >
      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.productCmsWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.productCmsWindowMessage, {
                cmsPagesStatus: cmsPagesStatusLabel,
                pageCount: cmsPages.length,
              })}
            </p>
          </div>
          <Link
            href={localizeHref("/cms", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.productCmsWindowCta}
          </Link>
        </div>
        <StorefrontSpotlightBoard
          culture={culture}
          cards={cmsCards}
          emptyMessage={formatResource(copy.productCmsWindowEmptyMessage, {
            status: cmsPagesStatusLabel,
          })}
        />
      </section>

      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.productCatalogWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.productCatalogWindowMessage, {
                categoriesStatus: categoriesStatusLabel,
                categoryCount: categories.length,
              })}
            </p>
          </div>
          <Link
            href={localizeHref("/catalog", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.productCatalogWindowCta}
          </Link>
        </div>
        <StorefrontSpotlightBoard
          culture={culture}
          cards={categoryCards}
          emptyMessage={formatResource(copy.productCatalogWindowEmptyMessage, {
            status: categoriesStatusLabel,
          })}
        />
      </section>

      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] lg:col-span-2">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.productProductsWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.productProductsWindowMessage, {
                productsStatus: productsStatusLabel,
                productCount: products.length,
              })}
            </p>
          </div>
          <Link
            href={localizeHref("/catalog", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.productProductsWindowCta}
          </Link>
        </div>
        <StorefrontOfferBoard
          culture={culture}
          cards={productCards}
          emptyMessage={formatResource(copy.productProductsWindowEmptyMessage, {
            status: productsStatusLabel,
          })}
        />
      </section>

      <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] lg:col-span-2">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
          {copy.campaignWindowPromotionLaneSectionTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.campaignWindowPromotionLaneSectionMessage}
        </p>
        <StorefrontCampaignBoard
          culture={culture}
          cards={promotionLaneCards}
          emptyMessage={formatResource(copy.campaignWindowPromotionLaneEmptyMessage, {
            productsStatus: productsStatusLabel,
          })}
        />
      </div>

      <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] lg:col-span-2">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
          {copy.productCartWindowTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {cartSummary
            ? formatResource(copy.productCartWindowMessage, {
                itemCount: cartSummary.itemCount,
                total: formatMoney(
                  cartSummary.grandTotalGrossMinor,
                  cartSummary.currency,
                  culture,
                ),
              })
            : copy.productCartWindowFallback}
        </p>
        <div className="mt-5 flex flex-wrap gap-3">
          <Link
            href={localizeHref("/cart", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.productCartWindowCartCta}
          </Link>
          <Link
            href={localizeHref("/checkout", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.productCartWindowCheckoutCta}
          </Link>
        </div>
      </div>
    </div>
  );
}
