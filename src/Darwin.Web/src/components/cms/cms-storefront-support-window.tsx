import Link from "next/link";
import { StorefrontCampaignBoard } from "@/components/storefront/storefront-campaign-board";
import { StorefrontSpotlightBoard } from "@/components/storefront/storefront-spotlight-board";
import { sortProductsByOpportunity } from "@/features/catalog/merchandising";
import { summarizeCatalogPromotionLanes } from "@/features/catalog/promotion-lanes";
import type { PublicCategorySummary, PublicProductSummary } from "@/features/catalog/types";
import { buildStorefrontCategorySpotlightLinkCards, buildStorefrontOfferCards } from "@/features/storefront/storefront-campaigns";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { toWebApiUrl } from "@/lib/webapi-url";
import {
  formatResource,
  getSharedResource,
  resolveApiStatusLabel,
} from "@/localization";

type CmsStorefrontSupportWindowProps = {
  culture: string;
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

export function CmsStorefrontSupportWindow({
  culture,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  cartSummary,
}: CmsStorefrontSupportWindowProps) {
  const copy = getSharedResource(culture);
  const categoriesStatusLabel = resolveApiStatusLabel(categoriesStatus, copy);
  const productsStatusLabel = resolveApiStatusLabel(productsStatus, copy);
  const rankedProducts = sortProductsByOpportunity(products);
  const categoryCards = buildStorefrontCategorySpotlightLinkCards(categories, {
    prefix: "cms-storefront-support",
    fallbackDescription: copy.cmsCatalogWindowFallbackDescription,
  });
  const promotionLaneCards = summarizeCatalogPromotionLanes(rankedProducts).map((entry) => {
    const laneLabel =
      entry.lane === "hero-offers"
        ? copy.cmsCampaignPromotionLaneHeroLabel
        : entry.lane === "value-offers"
          ? copy.cmsCampaignPromotionLaneValueLabel
          : entry.lane === "live-offers"
            ? copy.cmsCampaignPromotionLaneLiveOffersLabel
            : copy.cmsCampaignPromotionLaneBaseLabel;
    const href =
      entry.lane === "hero-offers"
        ? buildAppQueryPath("/catalog", { visibleState: "offers", visibleSort: "offers-first", savingsBand: "hero" })
        : entry.lane === "value-offers"
          ? buildAppQueryPath("/catalog", { visibleState: "offers", visibleSort: "offers-first", savingsBand: "value" })
          : entry.lane === "live-offers"
            ? buildAppQueryPath("/catalog", { visibleState: "offers", visibleSort: "savings-desc" })
            : buildAppQueryPath("/catalog", { visibleState: "base", visibleSort: "base-first" });

    return {
      id: `cms-support-promotion-${entry.lane}`,
      label: copy.cmsCampaignPromotionLaneCardLabel,
      title: entry.anchorProduct
        ? formatResource(copy.cmsCampaignPromotionLaneTitle, {
            lane: laneLabel,
            product: entry.anchorProduct.name,
          })
        : formatResource(copy.cmsCampaignPromotionLaneFallbackTitle, {
            lane: laneLabel,
          }),
      description: entry.anchorProduct
        ? formatResource(copy.cmsCampaignPromotionLaneDescription, {
            lane: laneLabel,
            count: entry.count,
            price: formatMoney(entry.anchorProduct.priceMinor, entry.anchorProduct.currency, culture),
          })
        : formatResource(copy.cmsCampaignPromotionLaneFallbackDescription, {
            lane: laneLabel,
          }),
      href,
      ctaLabel: copy.cmsCampaignPromotionLaneCta,
      meta: formatResource(copy.cmsCampaignPromotionLaneMeta, {
        count: entry.count,
      }),
    };
  });
  const productCards = buildStorefrontOfferCards(
    rankedProducts,
    {
      labels: {
        heroOffer: copy.cmsCampaignProductHeroLabel,
        valueOffer: copy.cmsCampaignProductValueLabel,
        priceDrop: copy.cmsCampaignProductPriceDropLabel,
        steadyPick: copy.cmsCampaignProductSteadyLabel,
      },
      formatPrice: (product) =>
        formatMoney(product.priceMinor, product.currency, culture),
      describeWithSavings: (_, input) =>
        formatResource(copy.cmsProductsWindowOfferDescription, {
          savingsPercent: input.savingsPercent,
          price: input.price,
        }),
      describeWithoutSavings: (product) =>
        product.shortDescription ?? copy.cmsProductsWindowFallbackDescription,
      fallbackDescription: copy.cmsProductsWindowFallbackDescription,
    },
  );

  return (
    <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.cmsCatalogWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.cmsCatalogWindowMessage, {
                categoriesStatus: categoriesStatusLabel ?? categoriesStatus,
                categoryCount: categories.length,
              })}
            </p>
          </div>
          <Link
            href={localizeHref("/catalog", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.cmsCatalogWindowCta}
          </Link>
        </div>
        <StorefrontSpotlightBoard
          culture={culture}
          cards={categoryCards}
          emptyMessage={formatResource(copy.cmsCatalogWindowEmptyMessage, {
            status: categoriesStatusLabel ?? categoriesStatus,
          })}
        />
      </section>

      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.cmsProductsWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.cmsProductsWindowMessage, {
                productsStatus: productsStatusLabel ?? productsStatus,
                productCount: products.length,
              })}
            </p>
          </div>
          <Link
            href={localizeHref("/catalog", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.cmsProductsWindowCta}
          </Link>
        </div>
        {productCards.length > 0 ? (
          <div className="mt-5 grid gap-3">
            {productCards.map((product, index) => {
              const sourceProduct = rankedProducts[index] ?? null;
              const productImageUrl = toWebApiUrl(sourceProduct?.primaryImageUrl ?? "");

              return (
                <Link
                  key={product.id}
                  href={localizeHref(product.href, culture)}
                  className="grid gap-3 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 transition hover:bg-[var(--color-surface-panel)] md:grid-cols-[72px_minmax(0,1fr)]"
                >
                  <div className="flex h-[72px] items-center justify-center rounded-[1rem] bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-2">
                    {productImageUrl ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img
                        src={productImageUrl}
                        alt={sourceProduct?.name ?? product.title}
                        className="max-h-14 w-auto object-contain"
                      />
                    ) : (
                      <span className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                        {copy.noImage}
                      </span>
                    )}
                  </div>
                  <div>
                    <p className="font-semibold text-[var(--color-text-primary)]">
                      {product.title}
                    </p>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {product.description}
                    </p>
                    <p className="mt-2 text-sm font-semibold text-[var(--color-text-primary)]">
                      {product.price}
                    </p>
                  </div>
                </Link>
              );
            })}
          </div>
        ) : (
          <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.cmsProductsWindowEmptyMessage, {
              status: productsStatusLabel ?? productsStatus,
            })}
          </p>
        )}
      </section>

      <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] lg:col-span-2">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
          {copy.cmsCampaignPromotionLaneSectionTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.cmsCampaignPromotionLaneSectionMessage}
        </p>
        <StorefrontCampaignBoard
          culture={culture}
          cards={promotionLaneCards}
          emptyMessage={formatResource(copy.cmsCampaignWindowEmptyMessage, {
            categoriesStatus: categoriesStatusLabel ?? categoriesStatus,
            productsStatus: productsStatusLabel ?? productsStatus,
          })}
        />
      </div>

      <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] lg:col-span-2">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
          {copy.cmsCartWindowTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {cartSummary
            ? formatResource(copy.cmsCartWindowMessage, {
                itemCount: cartSummary.itemCount,
                total: formatMoney(
                  cartSummary.grandTotalGrossMinor,
                  cartSummary.currency,
                  culture,
                ),
              })
            : copy.cmsCartWindowFallback}
        </p>
        <div className="mt-5 flex flex-wrap gap-3">
          <Link
            href={localizeHref("/cart", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.cmsCartWindowCartCta}
          </Link>
          <Link
            href={localizeHref("/checkout", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.cmsCartWindowCheckoutCta}
          </Link>
        </div>
      </div>
    </div>
  );
}



