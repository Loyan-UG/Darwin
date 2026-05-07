import Link from "next/link";
import { StorefrontCampaignBoard } from "@/components/storefront/storefront-campaign-board";
import { StorefrontOfferBoard } from "@/components/storefront/storefront-offer-board";
import { StorefrontSpotlightBoard } from "@/components/storefront/storefront-spotlight-board";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import { summarizeCatalogPromotionLanes } from "@/features/catalog/promotion-lanes";
import {
  buildStorefrontCategoryCampaignCards,
  buildStorefrontCategorySpotlightLinkCards,
  buildStorefrontOfferCards,
  buildStorefrontPageSpotlightCards,
  buildStorefrontProductCampaignCards,
} from "@/features/storefront/storefront-campaigns";
import { buildStorefrontSpotlightSelections } from "@/features/storefront/storefront-spotlight";
import { formatMoney } from "@/lib/formatting";
import {
  formatResource,
  getCommerceResource,
  resolveApiStatusLabel,
} from "@/localization";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";

type CommerceStorefrontWindowProps = {
  culture: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
  title?: string;
  description?: string;
};

export function CommerceStorefrontWindow({
  culture,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  title,
  description,
}: CommerceStorefrontWindowProps) {
  const copy = getCommerceResource(culture);
  const cmsPagesStatusLabel =
    resolveApiStatusLabel(cmsPagesStatus, copy) ?? cmsPagesStatus;
  const categoriesStatusLabel =
    resolveApiStatusLabel(categoriesStatus, copy) ?? categoriesStatus;
  const productsStatusLabel =
    resolveApiStatusLabel(productsStatus, copy) ?? productsStatus;
  const {
    offerBoardProducts: offerBoard,
    campaignCategories,
    campaignProducts,
  } = buildStorefrontSpotlightSelections({
    cmsPages,
    categories,
    products,
    categoryCampaignCount: 2,
    productCampaignCount: 2,
    offerBoardCount: 3,
  });
  const campaignLabels = {
    heroOffer: copy.offerCampaignHeroLabel,
    valueOffer: copy.offerCampaignValueLabel,
    priceDrop: copy.offerCampaignPriceDropLabel,
    steadyPick: copy.offerCampaignSteadyLabel,
  };
  const promotionLaneCards = summarizeCatalogPromotionLanes(offerBoard).map(
    (entry) => {
      const laneLabel =
        entry.lane === "hero-offers"
          ? copy.storefrontWindowPromotionLaneHeroLabel
          : entry.lane === "value-offers"
            ? copy.storefrontWindowPromotionLaneValueLabel
            : entry.lane === "live-offers"
              ? copy.storefrontWindowPromotionLaneLiveOffersLabel
              : copy.storefrontWindowPromotionLaneBaseLabel;
      const href =
        entry.lane === "hero-offers"
          ? buildAppQueryPath("/catalog", {
              visibleState: "offers",
              visibleSort: "offers-first",
              savingsBand: "hero",
            })
          : entry.lane === "value-offers"
            ? buildAppQueryPath("/catalog", {
                visibleState: "offers",
                visibleSort: "offers-first",
                savingsBand: "value",
              })
            : entry.lane === "live-offers"
              ? buildAppQueryPath("/catalog", {
                  visibleState: "offers",
                  visibleSort: "savings-desc",
                })
              : buildAppQueryPath("/catalog", {
                  visibleState: "base",
                  visibleSort: "base-first",
                });

      return {
        id: `commerce-promotion-lane-${entry.lane}`,
        label: copy.storefrontWindowPromotionLaneCardLabel,
        title: entry.anchorProduct
          ? formatResource(copy.storefrontWindowPromotionLaneTitle, {
              lane: laneLabel,
              product: entry.anchorProduct.name,
            })
          : formatResource(copy.storefrontWindowPromotionLaneFallbackTitle, {
              lane: laneLabel,
            }),
        description:
          entry.anchorProduct !== null
            ? formatResource(copy.storefrontWindowPromotionLaneDescription, {
                lane: laneLabel,
                count: entry.count,
                price: formatMoney(
                  entry.anchorProduct.priceMinor,
                  entry.anchorProduct.currency,
                  culture,
                ),
              })
            : formatResource(copy.storefrontWindowPromotionLaneFallbackDescription, {
                lane: laneLabel,
              }),
        href,
        ctaLabel: copy.storefrontWindowPromotionLaneCta,
        meta: formatResource(copy.storefrontWindowPromotionLaneMeta, {
          count: entry.count,
        }),
      };
    },
  );
  const campaignBoard = [
    ...buildStorefrontCategoryCampaignCards(campaignCategories, {
      prefix: "commerce-campaign",
      label: copy.storefrontWindowCampaignCategoryLabel,
      fallbackDescription:
        copy.storefrontWindowCampaignCategoryFallbackDescription,
      ctaLabel: copy.storefrontWindowCampaignCategoryCta,
    }),
    ...buildStorefrontProductCampaignCards(campaignProducts, {
      prefix: "commerce-campaign",
      labels: campaignLabels,
      formatPrice: (product) =>
        formatMoney(product.priceMinor, product.currency, culture),
      describeWithSavings: (_, input) =>
        formatResource(copy.storefrontWindowCampaignProductDescription, {
          savingsPercent: input.savingsPercent,
          price: input.price,
        }),
      describeWithoutSavings: (_, input) =>
        formatResource(copy.storefrontWindowCampaignProductFallbackDescription, {
          price: input.price,
        }),
      ctaLabel: copy.storefrontWindowCampaignProductCta,
    }),
  ];
  const offerBoardCards = buildStorefrontOfferCards(offerBoard, {
    labels: campaignLabels,
    formatPrice: (product) =>
      formatMoney(product.priceMinor, product.currency, culture),
    describeWithSavings: (_, input) =>
      formatResource(copy.storefrontWindowProductOfferBoardDescription, {
        campaignLabel: input.campaignLabel,
        savingsPercent: input.savingsPercent,
        price: input.price,
      }),
    describeWithoutSavings: (product) =>
      product.shortDescription ??
      copy.storefrontWindowProductOfferBoardFallbackDescription,
    fallbackDescription: copy.storefrontWindowProductOfferBoardFallbackDescription,
    formatMeta: (product) =>
      typeof product.compareAtPriceMinor === "number"
        ? formatResource(copy.storefrontWindowProductOfferBoardMeta, {
            compareAt: formatMoney(
              product.compareAtPriceMinor,
              product.currency,
              culture,
            ),
          })
        : null,
    ctaLabel: copy.storefrontWindowProductSpotlightCta,
  });
  const cmsSpotlightCards = buildStorefrontPageSpotlightCards(cmsPages.slice(0, 1), {
    prefix: "commerce-window",
    fallbackDescription: copy.storefrontWindowCmsFallbackMessage,
  });
  const categorySpotlightCards = buildStorefrontCategorySpotlightLinkCards(
    categories.slice(0, 1),
    {
      prefix: "commerce-window",
      fallbackDescription: copy.storefrontWindowCatalogFallbackMessage,
    },
  );
  const cmsSpotlightCard = cmsSpotlightCards[0] ?? null;
  const categorySpotlightCard = categorySpotlightCards[0] ?? null;

  return (
    <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
        {title ?? copy.storefrontWindowTitle}
      </p>
      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
        {description ??
          formatResource(copy.storefrontWindowMessage, {
            cmsPagesStatus: cmsPagesStatusLabel,
            categoriesStatus: categoriesStatusLabel,
            productsStatus: productsStatusLabel,
            cmsCount: cmsPages.length,
            categoryCount: categories.length,
            productCount: products.length,
          })}
      </p>

      <div className="mt-5 grid gap-4 xl:grid-cols-3">
        <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
            {copy.storefrontWindowCmsLabel}
          </p>
          <p className="mt-3 text-lg font-semibold text-[var(--color-text-primary)]">
            {cmsSpotlightCard?.title ?? copy.storefrontWindowCmsFallbackTitle}
          </p>
          <div className="mt-4 flex flex-wrap gap-3">
            <Link
              href={localizeHref("/cms", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            >
              {copy.storefrontWindowCmsIndexCta}
            </Link>
          </div>
          <StorefrontSpotlightBoard
            culture={culture}
            cards={cmsSpotlightCard ? [cmsSpotlightCard] : []}
            emptyMessage={copy.storefrontWindowCmsFallbackMessage}
          />
        </article>

        <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
            {copy.storefrontWindowCatalogLabel}
          </p>
          <p className="mt-3 text-lg font-semibold text-[var(--color-text-primary)]">
            {categorySpotlightCard?.title ?? copy.storefrontWindowCatalogFallbackTitle}
          </p>
          <div className="mt-4 flex flex-wrap gap-3">
            <Link
              href={localizeHref("/catalog", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            >
              {copy.storefrontWindowCatalogIndexCta}
            </Link>
          </div>
          <StorefrontSpotlightBoard
            culture={culture}
            cards={categorySpotlightCard ? [categorySpotlightCard] : []}
            emptyMessage={copy.storefrontWindowCatalogFallbackMessage}
          />
        </article>

        <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
            {copy.storefrontWindowProductLabel}
          </p>
          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.storefrontWindowProductOfferBoardMessage, {
              productCount: products.length,
              status: productsStatusLabel,
            })}
          </p>
          <div className="mt-4 flex flex-wrap gap-3">
            <Link
              href={localizeHref("/catalog", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            >
              {copy.storefrontWindowProductOfferBoardCta}
            </Link>
          </div>
          <StorefrontOfferBoard
            culture={culture}
            cards={offerBoardCards}
            emptyMessage={formatResource(copy.storefrontWindowProductOfferBoardEmptyMessage, {
              status: productsStatusLabel,
            })}
            columnsClassName="grid-cols-1"
          />
        </article>
      </div>

      <div className="mt-5 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
        <div className="flex items-center justify-between gap-3">
          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
            {copy.storefrontWindowCampaignTitle}
          </p>
          <Link
            href={localizeHref("/catalog", culture)}
            className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
          >
            {copy.storefrontWindowCampaignCta}
          </Link>
        </div>
        <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
          {formatResource(copy.storefrontWindowCampaignMessage, {
            categoryCount: categories.length,
            productCount: products.length,
          })}
        </p>
        <div className="mt-4 rounded-[1rem] bg-[var(--color-surface-panel)] px-4 py-4">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
            {copy.storefrontWindowPromotionLaneSectionTitle}
          </p>
          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.storefrontWindowPromotionLaneSectionMessage}
          </p>
          <StorefrontCampaignBoard
            culture={culture}
            cards={promotionLaneCards}
            emptyMessage={formatResource(copy.storefrontWindowPromotionLaneEmptyMessage, {
              productsStatus: productsStatusLabel,
            })}
            cardClassName="bg-[var(--color-surface-panel-strong)]"
          />
        </div>
        <StorefrontCampaignBoard
          culture={culture}
          cards={campaignBoard}
          emptyMessage={copy.storefrontWindowCampaignEmptyMessage}
        />
      </div>

      <div className="mt-5 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
        <p className="text-sm font-semibold text-[var(--color-text-primary)]">
          {copy.storefrontWindowCartTitle}
        </p>
        <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.storefrontWindowCartMessage}
        </p>
        <div className="mt-4 flex flex-wrap gap-3">
          <Link
            href={localizeHref("/cart", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
          >
            {copy.storefrontWindowCartCartCta}
          </Link>
          <Link
            href={localizeHref("/checkout", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
          >
            {copy.storefrontWindowCartCheckoutCta}
          </Link>
        </div>
      </div>
    </section>
  );
}

